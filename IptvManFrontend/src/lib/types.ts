export interface FilterSettings {
	adultFilter: boolean;
	categoryFilters: string[];
	id?: number;
}

export interface Account {
	id: string;
	host: string;
	username?: string;
	password?: string;
	filterSettings: FilterSettings;
}

export interface HealthResponse {
	status: string;
	timestamp: string;
}
