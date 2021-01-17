# MarEx

## Description
MarEx is a tool that allows you to unpack the contents of a MAR file to a folder, and lets you pack a folder to a MAR file.

MAR (MSN Archive) is a proprietary file format, used by [MSN Explorer](https://en.wikipedia.org/wiki/MSN_Dial-up#MSN_Explorer).

The idea of this tool came to mind after looking for a way to tinker with MSN Explorer and coming across this [Neowin forum topic](https://www.neowin.net/forum/topic/35549-skinning-msn-explorer/).

MarEx has been featured in a [YouTube video](https://www.youtube.com/watch?v=PRItbF1BVh8) by [Michael MJD](https://www.youtube.com/user/mjd7999)

## Examples
Unpacking the contents of a MAR file to a folder:

```MarEx.exe d -i "C:\Temp\file.mar" -o "C:\Temp\myfolder"```

Packing the contents of a folder to a MAR file:

```MarEx.exe c -i "C:\Temp\myfolder" -o "C:\Temp\file.mar"```

Listing the table of contents of a MAR file:

```MarEx.exe i "C:\Temp\file.mar"```

## Known MAR files
The MAR files used by MSN Explorer version 6 and 7 can be found under ```C:\Program Files\MSN Explorer\MSNCoreFiles```:
* highcont.mar
* mail.mar
* market.mar
* signup.mar
* themedef.mar
* ui.mar

## Issues
While the packing function returns an exact copy of the signup.mar file, some other files are not identical. This should be fixed as MSN Explorer will refuse to load MAR files that are not structured correctly.

## Remarks
Some installations of MSN Explorer contain a hidden "settingsd.htm" file in signup.mar, which allows you to create and remove users.
