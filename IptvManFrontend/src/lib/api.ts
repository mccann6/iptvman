import type { Account, FilterSettings, HealthResponse } from './types';

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
