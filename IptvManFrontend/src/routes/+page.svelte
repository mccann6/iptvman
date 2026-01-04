<script lang="ts">
	import { onMount } from 'svelte';
	import { 
		getAccounts, 
		getHealth, 
		createAccount, 
		updateAccount, 
		deleteAccount,
		initializeCategories,
		refreshLiveCategories,
		refreshVodCategories,
		refreshSeriesCategories,
		updateLiveCategories,
		updateVodCategories,
		updateSeriesCategories,
		getPlayerCategories,
		getLiveStreams,
		getChannelMappings,
		createChannelMapping,
		updateChannelMapping,
		deleteChannelMapping,
		deleteAllChannelMappings
	} from '$lib/api';
	import type { Account, HealthResponse, Category, CategoryRefreshResult, LiveStream, ChannelMapping, PaginationInfo } from '$lib/types';

	let accounts = $state<Account[]>([]);
	let health = $state<HealthResponse | null>(null);
	let loading = $state(true);
	let error = $state<string | null>(null);
	let showModal = $state(false);
	let showCategoryModal = $state(false);
	let showChannelModal = $state(false);
	let editingAccount = $state<Account | null>(null);
	let managingAccount = $state<Account | null>(null);
	let categoryType = $state<'live' | 'vod' | 'series'>('live');
	let categories = $state<Category[]>([]);
	let categoryLoading = $state(false);
	let categorySearchQuery = $state('');
	let selectedCategories = $state<Set<string>>(new Set());
	let refreshResult = $state<CategoryRefreshResult | null>(null);
	
	let filteredCategories = $derived(
		categorySearchQuery.trim() === ''
			? categories
			: categories.filter(cat => 
				cat.category_name.toLowerCase().includes(categorySearchQuery.toLowerCase()) ||
				cat.category_id.toLowerCase().includes(categorySearchQuery.toLowerCase())
			)
	);
	
	// Channel Management State
	let liveStreams = $state<LiveStream[]>([]);
	let channelMappings = $state<ChannelMapping[]>([]);
	let filteredStreams = $state<LiveStream[]>([]);
	let channelLoading = $state(false);
	let searchQuery = $state('');
	let editingChannels = $state<Map<number, ChannelMapping>>(new Map());
	let editingSignal = $state(0); // Force reactivity when editingChannels changes
	let channelSaveStatus = $state<string | null>(null);
	let paginationInfo = $state<PaginationInfo | null>(null);
	let currentPage = $state(1);
	let pageSize = $state(100);
	let selectedCategoryId = $state<string>('');
	let liveCategories = $state<Category[]>([]);
	let showDisabledCategories = $state(false);

	// Form state
	let formData = $state({
		id: '',
		host: '',
		username: '',
		password: '',
		adultFilter: false
	});

	onMount(async () => {
		await loadData();
	});

	async function loadData() {
		try {
			loading = true;
			const [accountsData, healthData] = await Promise.all([
				getAccounts(),
				getHealth()
			]);
			accounts = accountsData;
			health = healthData;
			error = null;
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to load data';
		} finally {
			loading = false;
		}
	}

	function openAddModal() {
		editingAccount = null;
		formData = {
			id: '',
			host: '',
			username: '',
			password: '',
			adultFilter: false
		};
		showModal = true;
	}

	function openEditModal(account: Account) {
		editingAccount = account;
		formData = {
			id: account.id,
			host: account.host,
			username: account.username || '',
			password: account.password || '',
			adultFilter: account.filterSettings.adultFilter
		};
		showModal = true;
	}

	async function handleSubmit() {
		try {
			const account: Account = {
				id: formData.id,
				host: formData.host,
				username: formData.username || undefined,
				password: formData.password || undefined,
				filterSettings: {
					adultFilter: formData.adultFilter,
					allowedLiveCategoryIds: [],
					notAllowedLiveCategoryIds: [],
					allowedVodCategoryIds: [],
					notAllowedVodCategoryIds: [],
					allowedSeriesCategoryIds: [],
					notAllowedSeriesCategoryIds: []
				}
			};

			if (editingAccount) {
				// Preserve existing filter settings when editing
				account.filterSettings = editingAccount.filterSettings;
				account.filterSettings.adultFilter = formData.adultFilter;
				await updateAccount(account.id, account);
			} else {
				await createAccount(account);
				// Initialize categories for new account
				if (account.username && account.password) {
					await initializeCategories(account.id, account.username, account.password);
				}
			}

			showModal = false;
			await loadData();
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to save account';
		}
	}

	async function handleDelete(id: string) {
		if (!confirm(`Are you sure you want to delete account "${id}"?`)) return;

		try {
			await deleteAccount(id);
			await loadData();
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to delete account';
		}
	}

	async function openCategoryManager(account: Account, type: 'live' | 'vod' | 'series') {
		managingAccount = account;
		categoryType = type;
		categoryLoading = true;
		categorySearchQuery = ''; // Reset search
		showCategoryModal = true;
		refreshResult = null;

		try {
			// Fetch categories from player API
			const action = type === 'live' ? 'get_live_categories' : 
						   type === 'vod' ? 'get_vod_categories' : 
						   'get_series_categories';
			
			categories = await getPlayerCategories(account.id, action, account.username, account.password);
			
			// Set initial selected state based on allowed IDs
			const allowedIds = type === 'live' ? account.filterSettings.allowedLiveCategoryIds :
							   type === 'vod' ? account.filterSettings.allowedVodCategoryIds :
							   account.filterSettings.allowedSeriesCategoryIds;
			
			selectedCategories = new Set(allowedIds);
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to load categories';
		} finally {
			categoryLoading = false;
		}
	}

	function toggleCategory(categoryId: string) {
		if (selectedCategories.has(categoryId)) {
			selectedCategories.delete(categoryId);
		} else {
			selectedCategories.add(categoryId);
		}
		selectedCategories = selectedCategories; // Trigger reactivity
	}

	function selectAll() {
		// Only select currently filtered/visible categories
		const newSelection = new Set(selectedCategories);
		filteredCategories.forEach(c => newSelection.add(c.category_id));
		selectedCategories = newSelection;
	}

	function deselectAll() {
		// Only deselect currently filtered/visible categories
		const newSelection = new Set(selectedCategories);
		filteredCategories.forEach(c => newSelection.delete(c.category_id));
		selectedCategories = newSelection;
	}

	async function saveCategories() {
		if (!managingAccount) return;

		try {
			const allCategoryIds = new Set(categories.map(c => c.category_id));
			const allowedIds = Array.from(selectedCategories);
			const notAllowedIds = Array.from(allCategoryIds).filter(id => !selectedCategories.has(id));

			const request = {
				allowedCategoryIds: allowedIds,
				notAllowedCategoryIds: notAllowedIds
			};

			if (categoryType === 'live') {
				await updateLiveCategories(managingAccount.id, request);
			} else if (categoryType === 'vod') {
				await updateVodCategories(managingAccount.id, request);
			} else {
				await updateSeriesCategories(managingAccount.id, request);
			}

			showCategoryModal = false;
			await loadData();
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to save categories';
		}
	}

	async function handleRefreshCategories() {
		if (!managingAccount) return;

		try {
			categoryLoading = true;
			
			if (categoryType === 'live') {
				refreshResult = await refreshLiveCategories(managingAccount.id, managingAccount.username, managingAccount.password);
			} else if (categoryType === 'vod') {
				refreshResult = await refreshVodCategories(managingAccount.id, managingAccount.username, managingAccount.password);
			} else {
				refreshResult = await refreshSeriesCategories(managingAccount.id, managingAccount.username, managingAccount.password);
			}

			// Reload categories
			const action = categoryType === 'live' ? 'get_live_categories' : 
						   categoryType === 'vod' ? 'get_vod_categories' : 
						   'get_series_categories';
			
			categories = await getPlayerCategories(managingAccount.id, action, managingAccount.username, managingAccount.password);
			
			await loadData();
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to refresh categories';
		} finally {
			categoryLoading = false;
		}
	}

	function getCategoryCount(account: Account, type: 'live' | 'vod' | 'series'): number {
		if (type === 'live') return account.filterSettings.allowedLiveCategoryIds.length;
		if (type === 'vod') return account.filterSettings.allowedVodCategoryIds.length;
		return account.filterSettings.allowedSeriesCategoryIds.length;
	}

	// Channel Management Functions
	async function openChannelManager(account: Account) {
		managingAccount = account;
		showChannelModal = true;
		editingChannels = new Map();
		channelSaveStatus = null;
		currentPage = 1;
		searchQuery = '';
		selectedCategoryId = '';
		showDisabledCategories = false;
		
		// Load categories first
		try {
			liveCategories = await getPlayerCategories(account.id, 'get_live_categories', account.username, account.password);
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to load categories';
		}
		
		await loadChannels(1);
	}

	async function loadChannels(page: number = currentPage) {
		if (!managingAccount) return;

		try {
			channelLoading = true;
			currentPage = page;
			const categoryFilter = selectedCategoryId || undefined;
			const [streamsResponse, mappings] = await Promise.all([
				getLiveStreams(managingAccount.id, managingAccount.username, managingAccount.password, categoryFilter, page, pageSize, showDisabledCategories),
				getChannelMappings(managingAccount.id)
			]);
			
			liveStreams = streamsResponse.streams;
			paginationInfo = streamsResponse.pagination;
			channelMappings = mappings;
			filterStreams();
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to load channels';
		} finally {
			channelLoading = false;
		}
	}

	async function handleCategoryChange() {
		currentPage = 1;
		searchQuery = '';
		await loadChannels(1);
	}

	async function handleToggleDisabledCategories() {
		currentPage = 1;
		searchQuery = '';
		await loadChannels(1);
	}

	function filterStreams() {
		// In paginated mode, we filter only the current page
		const query = searchQuery.toLowerCase();
		filteredStreams = liveStreams.filter(stream => 
			stream.name.toLowerCase().includes(query) ||
			stream.category_id?.toLowerCase().includes(query)
		);
	}

	function getMapping(streamId: number): ChannelMapping | undefined {
		return channelMappings.find(m => m.originalStreamId === streamId.toString());
	}

	function getEditingMapping(streamId: number): ChannelMapping {
		if (editingChannels.has(streamId)) {
			return editingChannels.get(streamId)!;
		}
		
		const existing = getMapping(streamId);
		if (existing) {
			return { ...existing };
		}
		
		return {
			accountId: managingAccount!.id,
			originalStreamId: streamId.toString(),
			isVisible: true,
			sortOrder: 0
		};
	}

	function updateEditingChannel(streamId: number, updates: Partial<ChannelMapping>) {
		const current = getEditingMapping(streamId);
		editingChannels.set(streamId, { ...current, ...updates });
		editingChannels = editingChannels; // CRITICAL: Reassign to trigger Svelte 5 reactivity
		editingSignal++; // Force reactive update
	}

	function toggleVisibility(streamId: number) {
		const current = getEditingMapping(streamId);
		updateEditingChannel(streamId, { isVisible: !current.isVisible });
	}

	function hasChanges(streamId: number): boolean {
		return editingChannels.has(streamId);
	}

	async function saveChannel(streamId: number) {
		if (!managingAccount) return;
		
		const mapping = editingChannels.get(streamId);
		if (!mapping) return;

		try {
			const existing = getMapping(streamId);
			
			if (existing) {
				await updateChannelMapping(managingAccount.id, existing.id!, mapping);
			} else {
				await createChannelMapping(managingAccount.id, mapping);
			}
			
			editingChannels.delete(streamId);
			editingChannels = editingChannels; // Trigger reactivity
			editingSignal++; // Force reactive update
			await loadChannels();
			channelSaveStatus = 'saved';
			setTimeout(() => channelSaveStatus = null, 2000);
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to save channel';
		}
	}

	async function resetChannel(streamId: number) {
		if (!managingAccount) return;
		
		const existing = getMapping(streamId);
		if (existing && existing.id) {
			try {
				await deleteChannelMapping(managingAccount.id, existing.id);
				editingChannels.delete(streamId);
				editingChannels = editingChannels; // Trigger reactivity
				editingSignal++; // Force reactive update
				await loadChannels();
				channelSaveStatus = 'reset';
				setTimeout(() => channelSaveStatus = null, 2000);
			} catch (e) {
				error = e instanceof Error ? e.message : 'Failed to reset channel';
			}
		} else {
			editingChannels.delete(streamId);
			editingChannels = editingChannels; // Trigger reactivity
			editingSignal++; // Force reactive update
		}
	}

	async function resetAllChannels() {
		if (!managingAccount || !confirm('Reset all channel customizations? This cannot be undone.')) return;
		
		try {
			channelLoading = true;
			await deleteAllChannelMappings(managingAccount.id);
			editingChannels = new Map();
			await loadChannels();
			channelSaveStatus = 'all-reset';
			setTimeout(() => channelSaveStatus = null, 2000);
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to reset all channels';
		} finally {
			channelLoading = false;
		}
	}

	// Watch for search query changes and filter current page
	$effect(() => {
		if (searchQuery !== undefined) {
			filterStreams();
		}
	});
</script>

<div class="min-h-screen bg-base-200">
	<div class="container mx-auto p-4">
		<!-- Header -->
		<div class="navbar bg-base-100 rounded-box shadow-lg mb-6">
			<div class="flex-1">
				<h1 class="text-3xl font-bold">IPTV Manager</h1>
			</div>
			<div class="flex-none gap-2">
				{#if health}
					<div class="badge badge-success gap-2">
						<div class="w-2 h-2 rounded-full bg-success-content"></div>
						{health.status}
					</div>
				{/if}
			</div>
		</div>

		<!-- Error Alert -->
		{#if error}
			<div class="alert alert-error mb-4">
				<svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6 shrink-0 stroke-current" fill="none" viewBox="0 0 24 24">
					<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z" />
				</svg>
				<span>{error}</span>
				<button class="btn btn-sm btn-ghost" onclick={() => error = null}>✕</button>
			</div>
		{/if}

		<!-- Stats & Add Button -->
		<div class="flex flex-col sm:flex-row gap-4 mb-6">
			<div class="stats shadow flex-1">
				<div class="stat">
					<div class="stat-title">Total Accounts</div>
					<div class="stat-value">{accounts.length}</div>
					<div class="stat-desc">IPTV providers configured</div>
				</div>
			</div>
			<div class="flex items-center">
				<button class="btn btn-primary" onclick={openAddModal}>
					<svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
					</svg>
					Add Account
				</button>
			</div>
		</div>

		<!-- Content -->
		{#if loading}
			<div class="flex justify-center items-center h-64">
				<span class="loading loading-spinner loading-lg"></span>
			</div>
		{:else if accounts.length === 0}
			<div class="card bg-base-100 shadow-xl">
				<div class="card-body text-center">
					<h2 class="text-2xl font-bold mb-2">No Accounts Yet</h2>
					<p class="mb-4">Get started by adding your first IPTV provider account</p>
					<button class="btn btn-primary btn-wide mx-auto" onclick={openAddModal}>Add Account</button>
				</div>
			</div>
		{:else}
			<div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
				{#each accounts as account}
					<div class="card bg-base-100 shadow-xl">
						<div class="card-body">
							<h2 class="card-title font-mono">{account.id}</h2>
							<div class="space-y-2 text-sm">
								<div>
									<span class="font-semibold">Host:</span>
									<span class="text-base-content/70 break-all">{account.host}</span>
								</div>
								<div>
									<span class="font-semibold">Username:</span>
									<span class="text-base-content/70">{account.username || 'Not set'}</span>
								</div>
								<div>
									<span class="font-semibold">Password:</span>
									<span class="text-base-content/70">{account.password ? '••••••••' : 'Not set'}</span>
								</div>
								<div class="flex gap-2 flex-wrap">
									{#if account.filterSettings.adultFilter}
										<span class="badge badge-sm badge-primary">Adult Filter</span>
									{/if}
								{#if getCategoryCount(account, 'live') > 0}
									<span class="badge badge-sm badge-secondary">
										Live: {getCategoryCount(account, 'live')}
									</span>
								{/if}
								{#if getCategoryCount(account, 'vod') > 0}
									<span class="badge badge-sm badge-accent">
										VOD: {getCategoryCount(account, 'vod')}
									</span>
								{/if}
								{#if getCategoryCount(account, 'series') > 0}
									<span class="badge badge-sm badge-info">
										Series: {getCategoryCount(account, 'series')}
									</span>
								{/if}
							</div>
						</div>
						<div class="divider my-2"></div>
						<div class="space-y-2">
							<button class="btn btn-sm btn-outline w-full" onclick={() => openCategoryManager(account, 'live')}>
								📺 Manage Live Categories
							</button>
							<button class="btn btn-sm btn-outline w-full" onclick={() => openCategoryManager(account, 'vod')}>
								🎬 Manage VOD Categories
							</button>
							<button class="btn btn-sm btn-outline w-full" onclick={() => openCategoryManager(account, 'series')}>
								📺 Manage Series Categories
							</button>
							<button class="btn btn-sm btn-primary w-full" onclick={() => openChannelManager(account)}>
								⚙️ Manage Channels
							</button>
						</div>
							<div class="card-actions justify-end mt-4">
								<button class="btn btn-sm btn-ghost" onclick={() => openEditModal(account)}>Edit</button>
								<button class="btn btn-sm btn-error btn-outline" onclick={() => handleDelete(account.id)}>Delete</button>
							</div>
						</div>
					</div>
				{/each}
			</div>
		{/if}
	</div>
</div>

<!-- Modal -->
{#if showModal}
	<div class="modal modal-open">
		<div class="modal-box max-w-2xl">
			<h3 class="font-bold text-lg mb-4">{editingAccount ? 'Edit Account' : 'Add New Account'}</h3>
			
			<form onsubmit={(e) => { e.preventDefault(); handleSubmit(); }}>
				<div class="space-y-4">
					<!-- ID -->
					<div class="form-control">
						<label class="label" for="id">
							<span class="label-text">Account ID *</span>
						</label>
						<input
							id="id"
							type="text"
							placeholder="e.g., MyProvider"
							class="input input-bordered"
							bind:value={formData.id}
							disabled={editingAccount !== null}
							required
						/>
						<label class="label">
							<span class="label-text-alt">Unique identifier for this account</span>
						</label>
					</div>

					<!-- Host -->
					<div class="form-control">
						<label class="label" for="host">
							<span class="label-text">Host URL *</span>
						</label>
						<input
							id="host"
							type="url"
							placeholder="http://iptv.example.com:8080"
							class="input input-bordered"
							bind:value={formData.host}
							required
						/>
					</div>

					<!-- Username -->
					<div class="form-control">
						<label class="label" for="username">
							<span class="label-text">Username</span>
						</label>
						<input
							id="username"
							type="text"
							placeholder="Optional"
							class="input input-bordered"
							bind:value={formData.username}
						/>
					</div>

					<!-- Password -->
					<div class="form-control">
						<label class="label" for="password">
							<span class="label-text">Password</span>
						</label>
						<input
							id="password"
							type="password"
							placeholder="Optional"
							class="input input-bordered"
							bind:value={formData.password}
						/>
					</div>

					<!-- Adult Filter -->
					<div class="form-control">
						<label class="label cursor-pointer justify-start gap-4">
							<input
								type="checkbox"
								class="checkbox"
								bind:checked={formData.adultFilter}
							/>
							<span class="label-text">Enable Adult Content Filter</span>
						</label>
					</div>
				</div>

				<div class="modal-action">
					<button type="button" class="btn" onclick={() => showModal = false}>Cancel</button>
					<button type="submit" class="btn btn-primary">
						{editingAccount ? 'Update' : 'Create'} Account
					</button>
				</div>
			</form>
		</div>
		<div class="modal-backdrop" onclick={() => showModal = false}></div>
	</div>
{/if}

<!-- Category Management Modal -->
{#if showCategoryModal && managingAccount}
	<div class="modal modal-open">
		<div class="modal-box max-w-4xl max-h-[90vh]">
			<h3 class="font-bold text-lg mb-4">
				Manage {categoryType === 'live' ? 'Live TV' : categoryType === 'vod' ? 'VOD' : 'Series'} Categories - {managingAccount.id}
			</h3>
			
			<!-- Refresh Result Alert -->
			{#if refreshResult && refreshResult.hasChanges}
				<div class="alert alert-info mb-4">
					<svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6 shrink-0 stroke-current" fill="none" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
					</svg>
					<span>{refreshResult.newCategories.length} new categories detected!</span>
				</div>
			{/if}
			
			<!-- Search Bar -->
			<div class="form-control mb-4">
				<input
					type="text"
					class="input input-bordered w-full"
					placeholder="Search categories (e.g., UK, Sports, Movies...)"
					bind:value={categorySearchQuery}
				/>
				{#if categorySearchQuery.trim()}
					<label class="label">
						<span class="label-text-alt">{filteredCategories.length} of {categories.length} categories shown</span>
					</label>
				{/if}
			</div>
			
			<!-- Actions Bar -->
			<div class="flex flex-wrap gap-2 mb-4">
				<button class="btn btn-sm btn-outline" onclick={handleRefreshCategories} disabled={categoryLoading}>
					🔄 Refresh from Server
				</button>
				<button class="btn btn-sm btn-outline" onclick={selectAll}>
					✓ Select All {categorySearchQuery.trim() ? 'Visible' : ''}
				</button>
				<button class="btn btn-sm btn-outline" onclick={deselectAll}>
					✗ Deselect All {categorySearchQuery.trim() ? 'Visible' : ''}
				</button>
				<div class="flex-1"></div>
				<div class="badge badge-lg">
					{selectedCategories.size} / {categories.length} selected
				</div>
			</div>
			
			{#if categoryLoading}
				<div class="flex justify-center items-center h-64">
					<span class="loading loading-spinner loading-lg"></span>
				</div>
			{:else if categories.length === 0}
				<div class="text-center py-8 text-base-content/60">
					No categories available
				</div>
			{:else}
				<!-- Category List -->
				<div class="overflow-y-auto max-h-96 border rounded-lg p-4 space-y-2">
					{#if filteredCategories.length === 0}
						<div class="text-center py-8 text-base-content/60">
							No categories match your search
						</div>
					{:else}
						{#each filteredCategories as category}
						<label class="flex items-center gap-3 p-3 hover:bg-base-200 rounded-lg cursor-pointer transition-colors">
							<input
								type="checkbox"
								class="checkbox"
								checked={selectedCategories.has(category.category_id)}
								onchange={() => toggleCategory(category.category_id)}
							/>
							<span class="flex-1">{category.category_name}</span>
							{#if refreshResult?.newCategories.some(c => c.category_id === category.category_id)}
								<span class="badge badge-success badge-sm">NEW</span>
							{/if}
						</label>
					{/each}
					{/if}
				</div>
			{/if}

			<div class="modal-action">
				<button type="button" class="btn" onclick={() => showCategoryModal = false}>Cancel</button>
				<button type="button" class="btn btn-primary" onclick={saveCategories} disabled={categoryLoading}>
					Save Changes
				</button>
			</div>
		</div>
		<div class="modal-backdrop" onclick={() => showCategoryModal = false}></div>
	</div>
{/if}

<!-- Channel Management Modal -->
{#if showChannelModal && managingAccount}
	<div class="modal modal-open">
		<div class="modal-box max-w-7xl max-h-[95vh] w-11/12">
			<h3 class="font-bold text-2xl mb-4">
				🎯 Channel Management - {managingAccount.id}
			</h3>
			
			<!-- Save Status Alert -->
			{#if channelSaveStatus}
				<div class="alert alert-success mb-4">
					<svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6 shrink-0 stroke-current" fill="none" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
					</svg>
					<span>
						{channelSaveStatus === 'saved' ? 'Channel saved successfully!' :
						 channelSaveStatus === 'reset' ? 'Channel reset to default!' :
						 'All channels reset successfully!'}
					</span>
				</div>
			{/if}
			
			<!-- Show Disabled Categories Toggle -->
			<div class="form-control mb-4">
				<label class="label cursor-pointer justify-start gap-3 w-fit">
					<input
						type="checkbox"
						class="toggle toggle-primary"
						bind:checked={showDisabledCategories}
						onchange={handleToggleDisabledCategories}
						disabled={channelLoading}
					/>
					<div>
						<span class="label-text font-semibold">Show channels from disabled categories</span>
						<div class="label-text-alt text-xs opacity-70">
							{showDisabledCategories 
								? 'Showing all channels (bypassing category filters)' 
								: 'Only showing channels from enabled categories'}
						</div>
					</div>
				</label>
			</div>
			
			<!-- Category Filter -->
			<div class="form-control mb-4">
				<label class="label">
					<span class="label-text font-semibold">📁 Filter by Category</span>
				</label>
				<select 
					class="select select-bordered w-full max-w-md"
					value={selectedCategoryId}
					onchange={async (e) => {
						selectedCategoryId = e.currentTarget.value;
						await handleCategoryChange();
					}}
					disabled={channelLoading}
				>
					<option value="">All Categories ({paginationInfo?.total_items ?? 0} channels)</option>
					{#each liveCategories as category}
						<option value={category.category_id}>{category.category_name}</option>
					{/each}
				</select>
			</div>
			
			<!-- Search and Actions Bar -->
			<div class="flex flex-wrap gap-2 mb-4">
				<div class="flex-1 min-w-64">
					<input
						type="text"
						placeholder="🔍 Search channels on current page..."
						class="input input-bordered w-full"
						bind:value={searchQuery}
					/>
					<p class="text-xs text-base-content/60 mt-1 ml-1">
						Searches only the {liveStreams.length} channels on the current page
					</p>
				</div>
				<button class="btn btn-outline btn-error" onclick={resetAllChannels} disabled={channelLoading || channelMappings.length === 0}>
					Reset All
				</button>
				<button class="btn btn-outline" onclick={() => loadChannels(currentPage)} disabled={channelLoading}>
					🔄 Refresh
				</button>
			</div>

			<!-- Stats -->
			<div class="stats stats-horizontal shadow mb-4 w-full">
				<div class="stat">
					<div class="stat-title">
						{selectedCategoryId ? 'Category' : 'Total'} Channels
					</div>
					<div class="stat-value text-2xl">{paginationInfo?.total_items ?? 0}</div>
					<div class="stat-desc">
						{selectedCategoryId ? 'in selected category' : 'across all categories'}
					</div>
				</div>
				<div class="stat">
					<div class="stat-title">Current Page</div>
					<div class="stat-value text-2xl">{currentPage} / {paginationInfo?.total_pages ?? 1}</div>
					<div class="stat-desc">showing {liveStreams.length} channels</div>
				</div>
				<div class="stat">
					<div class="stat-title">Customized</div>
					<div class="stat-value text-2xl">{channelMappings.length}</div>
				</div>
				<div class="stat">
					<div class="stat-title">Hidden</div>
					<div class="stat-value text-2xl">{channelMappings.filter(m => !m.isVisible).length}</div>
				</div>
				<div class="stat">
					<div class="stat-title">Pending Changes</div>
					<div class="stat-value text-2xl">{editingChannels.size}</div>
				</div>
			</div>
			
			{#if channelLoading}
				<div class="flex justify-center items-center h-64">
					<span class="loading loading-spinner loading-lg"></span>
				</div>
			{:else if filteredStreams.length === 0}
				<div class="text-center py-8 text-base-content/60">
					{searchQuery ? 'No channels match your search' : 'No channels available'}
				</div>
			{:else}
				<!-- Channel Table -->
				<div class="overflow-x-auto">
					<table class="table table-sm table-zebra">
						<thead>
							<tr>
								<th class="w-12">Visible</th>
								<th class="w-20">ID</th>
								<th>Channel Name</th>
								<th>Custom Name</th>
								<th class="w-24">Sort Order</th>
								<th class="w-32">Actions</th>
							</tr>
						</thead>
						<tbody>
							{#each filteredStreams as stream (stream.stream_id)}
								{@const _signal = editingSignal} <!-- Force reactivity -->
								{@const originalMapping = getMapping(stream.stream_id)}
								{@const isEditing = _signal >= 0 && editingChannels.has(stream.stream_id)} <!-- Use _signal to force dependency -->
								{@const editedMapping = isEditing ? editingChannels.get(stream.stream_id)! : getEditingMapping(stream.stream_id)}
								{@const _ = stream.stream_id === filteredStreams[0]?.stream_id && console.log('🎨 Rendering first row:', { 
									streamId: stream.stream_id, 
									signal: _signal,
									isEditing, 
									hasInMap: editingChannels.has(stream.stream_id),
									mapSize: editingChannels.size
								})}
								<tr class:bg-warning={isEditing} class:opacity-50={!editedMapping.isVisible}>
									<!-- Visibility Toggle -->
									<td>
										<input
											type="checkbox"
											class="toggle toggle-success toggle-sm"
											checked={editedMapping.isVisible}
											onchange={() => toggleVisibility(stream.stream_id)}
										/>
									</td>
									
									<!-- Stream ID -->
									<td class="font-mono text-xs">{stream.stream_id}</td>
									
									<!-- Original Name -->
									<td class="max-w-xs truncate" title={stream.name}>
										{stream.name}
									</td>
									
									<!-- Custom Name Input -->
									<td>
										<input
											type="text"
											class="input input-xs input-bordered w-full"
											placeholder="Custom name..."
											value={editedMapping.customName || ''}
											oninput={(e) => {
												console.log('⌨️ Input event fired for stream:', stream.stream_id, 'value:', e.currentTarget.value);
												updateEditingChannel(stream.stream_id, { customName: e.currentTarget.value });
											}}
										/>
									</td>
									<!-- Sort Order Input -->
									<td>
										<input
											type="number"
											class="input input-xs input-bordered w-20"
											placeholder="0"
											value={editedMapping.sortOrder}
											oninput={(e) => updateEditingChannel(stream.stream_id, { sortOrder: parseInt(e.currentTarget.value) || 0 })}
										/>
									</td>
									
									<!-- Action Buttons -->
									<td>
										<div class="flex gap-1">
											{#if isEditing}
												<button
													class="btn btn-xs btn-success"
													onclick={() => saveChannel(stream.stream_id)}
													title="Save changes"
												>
													💾
												</button>
												<button
													class="btn btn-xs btn-ghost"
													onclick={() => editingChannels.delete(stream.stream_id) && (editingChannels = editingChannels)}
													title="Cancel changes"
												>
													✕
												</button>
											{:else if originalMapping}
												<button
													class="btn btn-xs btn-warning"
													onclick={() => resetChannel(stream.stream_id)}
													title="Reset to default"
												>
													↺
												</button>
											{/if}
										</div>
									</td>
								</tr>
							{/each}
						</tbody>
					</table>
				</div>
			{/if}

			<!-- Pagination Controls -->
			{#if paginationInfo && paginationInfo.total_pages > 1}
				<div class="flex justify-center items-center gap-2 mt-4">
					<button 
						class="btn btn-sm" 
						onclick={() => loadChannels(1)}
						disabled={currentPage === 1 || channelLoading}
					>
						⏮ First
					</button>
					<button 
						class="btn btn-sm" 
						onclick={() => loadChannels(currentPage - 1)}
						disabled={currentPage === 1 || channelLoading}
					>
						◀ Prev
					</button>
					
					<div class="flex items-center gap-2">
						<span class="text-sm">Page</span>
						<input
							type="number"
							class="input input-sm input-bordered w-20 text-center"
							min="1"
							max={paginationInfo.total_pages}
							value={currentPage}
							onchange={(e) => {
								const page = parseInt(e.currentTarget.value);
								if (page >= 1 && page <= paginationInfo!.total_pages) {
									loadChannels(page);
								}
							}}
						/>
						<span class="text-sm">of {paginationInfo.total_pages}</span>
					</div>
					
					<button 
						class="btn btn-sm" 
						onclick={() => loadChannels(currentPage + 1)}
						disabled={currentPage === paginationInfo.total_pages || channelLoading}
					>
						Next ▶
					</button>
					<button 
						class="btn btn-sm" 
						onclick={() => loadChannels(paginationInfo.total_pages)}
						disabled={currentPage === paginationInfo.total_pages || channelLoading}
					>
						Last ⏭
					</button>
					
					<select 
						class="select select-sm select-bordered ml-4"
						value={pageSize}
						onchange={async (e) => {
							pageSize = parseInt(e.currentTarget.value);
							currentPage = 1;
							await loadChannels(1);
						}}
					>
						<option value={50}>50 per page</option>
						<option value={100}>100 per page</option>
						<option value={250}>250 per page</option>
						<option value={500}>500 per page</option>
					</select>
				</div>
			{/if}

			<div class="modal-action">
				<button type="button" class="btn" onclick={() => showChannelModal = false}>Close</button>
			</div>
		</div>
		<div class="modal-backdrop" onclick={() => showChannelModal = false}></div>
	</div>
{/if}
