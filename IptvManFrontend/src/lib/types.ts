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

export interface LiveStream {
	num: number;
	name: string;
	stream_type: string;
	stream_id: number;
	stream_icon: string;
	epg_channel_id: string;
	added: string;
	custom_sid: string;
	tv_archive: number;
	direct_source: string;
	tv_archive_duration: number;
	category_id: string;
	category_ids: number[];
	thumbnail: string;
}

export interface ChannelMapping {
	id?: number;
	accountId: string;
	originalStreamId: string;
	customName?: string;
	customGroupName?: string;
	isVisible: boolean;
	sortOrder: number;
	channelNumber?: number;
}

export interface PaginationInfo {
	current_page: number;
	page_size: number;
	total_items: number;
	total_pages: number;
}

export interface PaginatedLiveStreamsResponse {
	streams: LiveStream[];
	pagination: PaginationInfo;
}
