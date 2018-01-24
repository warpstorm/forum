# Warpstorm.com Forum

This message board solution is custom built specifically for Warpstorm.com. You will find many Warpstorm-specific references throughout the code and content. I am more than willing, however, to receive pull requests and help make adjustments that will make the code base more usable for other sites.

Anything in this project which is not licensed and written by me is open and free, so you're welcome to steal anything and everything I've written here. If you do, you would help me feel better about all my time spent by simply emailing me at yarbro@outlook.com to let me know you found some value.

In order to use the BBC code tag processor, this solution requires [CodeKicker.BBCode](http://codekicker.de/) and the source from that project is included in this one, albeit heavily reformatted by me and modernized to work with ASP.NET Core and the latest C# features. To remove this encumbering library, you will need to remove any tag processing or write your own tag processor and add it to the message processing service.

In order to use the identicons provided, the source for Jdenticon is currently included because the Nuget package won't install correctly. The license for that code is in the same folder as the code itself. To remove this encumbering library, feel free to delete the library folder and remove references in the layouts.