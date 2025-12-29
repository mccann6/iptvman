import type { Account, FilterSettings, HealthResponse, Category, CategoryRefreshResult, UpdateCategoriesRequest } from './types';

const API_BASE = '/api';

export async function getAccounts(): Promise<Account[]> {
	const response = await fetch(`${API_BASE}/accounts`);
	if (!response.ok) throw new Error('Failed to fetch accounts');
	return response.json();
}

export async function getAccount(id: string): Promise<Account> {
	const response = await fetch(`${API_BASE}/accounts/${id}`);
	if (!response.ok) throw new Error('Failed to fetch account');
	return response.json();
}

export async function createAccount(account: Account): Promise<Account> {
	const response = await fetch(`${API_BASE}/accounts`, {
		method: 'POST',
		headers: { 'Content-Type': 'application/json' },
		body: JSON.stringify(account)
	});
	if (!response.ok) {
		const error = await response.text();
		throw new Error(error || 'Failed to create account');
	}
	return response.json();
}

export async function updateAccount(id: string, account: Account): Promise<void> {
	const response = await fetch(`${API_BASE}/accounts/${id}`, {
		method: 'PUT',
		headers: { 'Content-Type': 'application/json' },
		body: JSON.stringify(account)
	});
	if (!response.ok) throw new Error('Failed to update account');
}

export async function deleteAccount(id: string): Promise<void> {
	const response = await fetch(`${API_BASE}/accounts/${id}`, {
		method: 'DELETE'
	});
	if (!response.ok) throw new Error('Failed to delete account');
}

export async function getFilters(): Promise<FilterSettings> {
	const response = await fetch(`${API_BASE}/filters`);
	if (!response.ok) throw new Error('Failed to fetch filters');
	return response.json();
}

export async function saveFilters(filters: FilterSettings): Promise<FilterSettings> {
	const response = await fetch(`${API_BASE}/filters`, {
		method: 'POST',
		headers: { 'Content-Type': 'application/json' },
		body: JSON.stringify(filters)
	});
	if (!response.ok) throw new Error('Failed to save filters');
	return response.json();
}

export async function clearCache(): Promise<string> {
	const response = await fetch(`${API_BASE}/cache/clear`, {
		method: 'POST'
	});
	if (!response.ok) throw new Error('Failed to clear cache');
	return response.text();
}

export async function getHealth(): Promise<HealthResponse> {
	const response = await fetch('/health');
	if (!response.ok) throw new Error('Health check failed');
	return response.json();
}

// Category Management Functions
export async function initializeCategories(
	accountId: string,
	username?: string,
	password?: string
): Promise<string> {
	const params = new URLSearchParams();
	if (username) params.append('username', username);
	if (password) params.append('password', password);
	
	const response = await fetch(`${API_BASE}/accounts/${accountId}/categories/initialize?${params}`, {
		method: 'POST'
	});
	if (!response.ok) throw new Error('Failed to initialize categories');
	return response.text();
}

export async function refreshLiveCategories(
	accountId: string,
	username?: string,
	password?: string
): Promise<CategoryRefreshResult> {
	const params = new URLSearchParams();
	if (username) params.append('username', username);
	if (password) params.append('password', password);
	
	const response = await fetch(`${API_BASE}/accounts/${accountId}/categories/live/refresh?${params}`, {
		method: 'POST'
	});
	if (!response.ok) throw new Error('Failed to refresh live categories');
	return response.json();
}

export async function refreshVodCategories(
	accountId: string,
	username?: string,
	password?: string
): Promise<CategoryRefreshResult> {
	const params = new URLSearchParams();
	if (username) params.append('username', username);
	if (password) params.append('password', password);
	
	const response = await fetch(`${API_BASE}/accounts/${accountId}/categories/vod/refresh?${params}`, {
		method: 'POST'
	});
	if (!response.ok) throw new Error('Failed to refresh VOD categories');
	return response.json();
}

export async function refreshSeriesCategories(
	accountId: string,
	username?: string,
	password?: string
): Promise<CategoryRefreshResult> {
	const params = new URLSearchParams();
	if (username) params.append('username', username);
	if (password) params.append('password', password);
	
	const response = await fetch(`${API_BASE}/accounts/${accountId}/categories/series/refresh?${params}`, {
		method: 'POST'
	});
	if (!response.ok) throw new Error('Failed to refresh series categories');
	return response.json();
}

export async function updateLiveCategories(
	accountId: string,
	request: UpdateCategoriesRequest
): Promise<void> {
	const response = await fetch(`${API_BASE}/accounts/${accountId}/categories/live`, {
		method: 'PUT',
		headers: { 'Content-Type': 'application/json' },
		body: JSON.stringify(request)
	});
	if (!response.ok) throw new Error('Failed to update live categories');
}

export async function updateVodCategories(
	accountId: string,
	request: UpdateCategoriesRequest
): Promise<void> {
	const response = await fetch(`${API_BASE}/accounts/${accountId}/categories/vod`, {
		method: 'PUT',
		headers: { 'Content-Type': 'application/json' },
		body: JSON.stringify(request)
	});
	if (!response.ok) throw new Error('Failed to update VOD categories');
}

export async function updateSeriesCategories(
	accountId: string,
	request: UpdateCategoriesRequest
): Promise<void> {
	const response = await fetch(`${API_BASE}/accounts/${accountId}/categories/series`, {
		method: 'PUT',
		headers: { 'Content-Type': 'application/json' },
		body: JSON.stringify(request)
	});
	if (!response.ok) throw new Error('Failed to update series categories');
}

// Get categories from player API
export async function getPlayerCategories(
	accountId: string,
	action: 'get_live_categories' | 'get_vod_categories' | 'get_series_categories',
	username?: string,
	password?: string
): Promise<Category[]> {
	const params = new URLSearchParams({ action, bypass_filters: 'true' });
	if (username) params.append('username', username);
	if (password) params.append('password', password);
	
	const response = await fetch(`/${accountId}/player_api.php?${params}`);
	if (!response.ok) throw new Error('Failed to fetch categories');
	return response.json();
}
