export interface FilterSettings {
	adultFilter: boolean;
	allowedLiveCategoryIds: string[];
	notAllowedLiveCategoryIds: string[];
	allowedVodCategoryIds: string[];
	notAllowedVodCategoryIds: string[];
	allowedSeriesCategoryIds: string[];
	notAllowedSeriesCategoryIds: string[];
	id?: number;
}

export interface Account {
	id: string;
	host: string;
	username?: string;
	password?: string;
	filterSettings: FilterSettings;
}

export interface Category {
	category_id: string;
	category_name: string;
	parent_id: number;
}

export interface CategoryRefreshResult {
	newCategories: Category[];
	hasChanges: boolean;
}

export interface UpdateCategoriesRequest {
	allowedCategoryIds: string[];
	notAllowedCategoryIds: string[];
}

export interface HealthResponse {
	status: string;
	timestamp: string;
}
