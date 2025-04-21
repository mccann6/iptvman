# IptvMan

A simple IPTV proxy with added caching and filtering.

# Usage

Pull the latest image from https://hub.docker.com/r/anthonymccann90/iptvman

Include at least the following ENV
```
ENV IPTV_ACCOUNTS="AccountName;http://www.account.com|AccountName2;http://www.account2.com"
```

Your accounts will be available using
- Host: localhost:port/AccountName
- Username: Your Account Username
- Password: Your Account Password

Other available ENVs
```
# semi colon separated values, keep only categorys that contain in category name
ENV CATEGORY_FILTERS="UK|; US|"
# filter out adult channels
ENV ADULT_FILTER="true"
# time in minutes to cache api calls
ENV CACHE_TIME="60"
time in minutes to cache epg xml file
ENV EPG_TIME="1440"
time in minutes to cache m3u file
ENV M3U_TIME="720"
```