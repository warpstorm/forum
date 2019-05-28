# nrrdio's Forum

A light-weight and mobile-friendly message board solution intended for small communities hosted in Microsoft Azure.<sup>[[1]](#notes)</sup>

Instructions for setup can be found [here](https://github.com/jyarbro/forum/wiki/Setup).

## Features

* Standard forum functionality such as boards, topics, messages, and user accounts.
* Robust client-side functionality using XHRs to enhance interactivity.
* Emphasis on progressive enhancement over graceful degradation.<sup>[[2]](#notes)</sup>
* Double posting and rapid posting spam prevention.
* Notifications for events that happened while users were away.
* Avatar and smiley image support using Azure Storage, with identicon fallback.
* One-touch simple responses to messages called "thoughts".
* Topic bookmarks, global topic pinning, and marking topics as unread allows users to revisit topics later.
* Imgur and YouTube integration enable simplified mobile usage and image sharing.
* ASP.NET Identity and Role integration.
* Individualized account settings, allowing users to pick their default landing page, number of messages per page, etc.
* Modular design allowing much of the code to be easily replaced or customized without significant impact to other areas.
* Static theme simplifies the development experience. No theme engine complexity here!

## Unlicensing

Anything in this project is open and free under the terms of the Unlicense, so you're welcome to use it however you want. See [the license](https://github.com/jyarbro/forum/blob/master/UNLICENSE) for specifics.

## Notes

0: yarbro@outlook.com

1: If you would like to use this software on another hosting platform besides Azure Web Applications, please submit an issue with specifics, or submit pull requests with modified code.

2: Progressive enhancement means we focus on getting basic functionality to the user up front in the most universally supported format possible. We then enhance the experience and build up the fanciness. This is opposed to the concept of graceful degradation, where you start fully featured and fail down to the lowest capable level. In practical terms, this means we do our best to make the site function even with javascript disabled. This is a goal, not a rule, so some features are not available without JS.
