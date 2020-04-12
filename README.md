# Amazon Ebook Getter
Script that 'buys' all Amazon.co.uk versions of the free ebooks currently on sale from Amazon.com

Right now there's a sale on Amazon.com whereby hundreds of ebooks have been made available for free. Rather than going through each ebook and buying their .co.uk counterpart, I made a script to do it for me.

## Prerequisites 
1. You'll need Firefox installed
2. You may need the latest .NET Framework runtime
3. This is only for users of the .co.uk Amazon domain!

## Usage
1. Open AmazonEbookGetter.exe
2. Give it the full path to your Firefox exe (it'll show an example)
3. It'll take you to the Amazon login page - when signing in remember to tick "Keep me signed in" so the script doesn't fail halfway through.
4. Once logged in, press any key in the console and it'll start!

There's roughly 60 pages of ebooks to go through, so at any time you can press a key in the console to cancel the script; it'll save the latest page it was on to start again from where you left off.

If you do want to close the program, please use the "Press any key to cancel/finish" feature! It'll ensure the Firefox browser and Selenium driver is closed cleanly.

Once cancelled, it'll ask you to wait for "Press enter to close" to appear, give it a few seconds and once you see that message it is safe to close the program.

## Troubleshooting
### Permission denied error on Firefox startup
Run the program as admin.
### The program randomly gets stuck midway through
This is a known issue with long-running programs and the Selenium Firefox driver - simply close the program & Firefox, and start it back up (it'll have saved the most recent page it's on).