# IptvMan

A simple IPTV proxy with added caching and filtering.

# Usage

Pull the latest image from https://hub.docker.com/r/anthonymccann90/iptvman

Include at least the following ENV
```
ENV="AccountName;http://www.account.com|AccountName2;http://www.account2.com"
```

Other available ENVs
```
# comma separated values, keep only categorys that contain in category name
CATEGORY_FILTERS="UK|, US|"
# filter out adult channels
ADULT_FILTER="true"
# time in minutes to cache api calls
CACHE_TIME="60"
time in minutes to cache epg xml file
EPG_TIME="1440"
time in minutes to cache m3u file
M3U_TIME="720"
```