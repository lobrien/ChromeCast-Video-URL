ChromeCast Video URL
==========================

Simplest app to cast a video URL to the ChromeCast from an iOS device

# Incomplete!
In order to use this, you'll need to build my [ChromeCast Xamarin iOS Binding](https://github.com/lobrien/ChromeCast-Xam-iOS-Binding)

In addition, you will need to have your [ChromeCast development system set up](http://www.knowing.net/index.php/2013/08/10/chromecast-home-media-server-xamarin-ios-ftw/):

- Via Google's ChromeCast "whitelisting" you'll need to set the constants WHITELISTED_URL and APPLICATION_GUID in `program.cs`
- At that URL, you'll have to have a ChromeCast receiver app (I just use [Google's sample](https://github.com/googlecast/cast-ios-sample/tree/master/receiver)) being served by a Web Server

This is the most trivial program I could write: you type in a video URL, press the button, and it casts it. There's no error handling, all output is to the console, etc. 

** I am pretty sure you need to run this from a physical device; I don't think you can do it via the emulator (I couldn't) ** 
