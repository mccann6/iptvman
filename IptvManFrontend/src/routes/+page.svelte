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
		getPlayerCategories
	} from '$lib/api';
	import type { Account, HealthResponse, Category, CategoryRefreshResult } from '$lib/types';

	let accounts = $state<Account[]>([]);
	let health = $state<HealthResponse | null>(null);
	let loading = $state(true);
	let error = $state<string | null>(null);
	let showModal = $state(false);
	let showCategoryModal = $state(false);
	let editingAccount = $state<Account | null>(null);
	let managingAccount = $state<Account | null>(null);
	let categoryType = $state<'live' | 'vod' | 'series'>('live');
	let categories = $state<Category[]>([]);
	let categoryLoading = $state(false);
	let selectedCategories = $state<Set<string>>(new Set());
	let refreshResult = $state<CategoryRefreshResult | null>(null);

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
		selectedCategories = new Set(categories.map(c => c.category_id));
	}

	function deselectAll() {
		selectedCategories = new Set();
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
			
			<!-- Actions Bar -->
			<div class="flex flex-wrap gap-2 mb-4">
				<button class="btn btn-sm btn-outline" onclick={handleRefreshCategories} disabled={categoryLoading}>
					🔄 Refresh from Server
				</button>
				<button class="btn btn-sm btn-outline" onclick={selectAll}>
					✓ Select All
				</button>
				<button class="btn btn-sm btn-outline" onclick={deselectAll}>
					✗ Deselect All
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
					{#each categories as category}
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
