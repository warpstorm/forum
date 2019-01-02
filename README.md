# Forum

A light-weight and mobile-friendly message board solution intended for small communities hosted in Microsoft Azure.<sup>[1]</sup>

## Features

* Standard forum functionality such as boards, topics, messages, and user accounts.
* Robust client-side functionality to enhance interactivity.
* Emphasis on progressive enhancement over graceful degradation.[3]
* Double posting and rapid posting spam prevention.
* Notifications for events that happened while users were away.
* Avatar and smiley image support using Azure Storage, with identicon fallback.
* Thoughts simplify the user experience by allowing one-touch simple responses to messages.
* Personal message pinning and marking topics as unread allows users to revisit topics later.
* Imgur and YouTube integration enable simplified mobile usage and image sharing.
* ASP.NET Identity and Role integration.
* BBCode Parser<sup>[2]</sup> for users who are comfortable with that. Can be replaced with other custom parsers easily.
* Individualized profile settings, allowing users to pick their default landing page, number of messages per page, etc.
* Modular design allowing much of the code to be easily replaced or customized.
* Static theme simplifies the development experience. No theme engine complexity here!

## Unlicensing and Usage

Anything in this project which is originally written by me is open and free, so you're welcome to use it however you want. If you do, you would help me feel better about all my time spent by simply emailing<sup>[0]</sup> me to let me know you found some value.

Instructions for setup can be found [here](https://github.com/jyarbro/forum/wiki/Setup).

## Notes

#### 0:
yarbro@outlook.com

#### 1:
If you would like to use this software on another hosting platform besides Azure Web Applications, please email<sup>[0]</sup> me with specifics.

#### 2:
In order to use the BBC code tag processor, this solution requires [CodeKicker.BBCode](http://codekicker.de/) and the source from that project is included in this one, albeit heavily reformatted by me and modernized to work with ASP.NET Core and the latest C# features. To remove this encumbering library, please find the `ParseBBC` method in the `MessageRepository` class.

#### 3:
Progressive enhancement means we focus on getting basic functionality to the user up front in the most universally supported format possible. Only then do we enhance the experience and build up the fanciness. This is opposed to the concept of graceful degradation, where you start fully featured and fail down to the lowest capable level. What this means for developers is this isn't a SPA and never will be.
