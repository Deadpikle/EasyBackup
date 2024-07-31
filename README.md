# Easy Backup

Easy Backup is a small cross-platform utility that allows you to copy files from one location to another or store them in an archive for the purposes of a backup. It's basically a fancy copy+paste utility.

Features include:

* Choose files or folders to backup (either via file dialogs or via drag & drop)
* Toggle copy of subdirectories on and off
* When copying a directory, you can choose to just copy the latest file out of that directory (by date modified)
* Save and load your backup locations as a template so that it's easy as pie to re-run a backup that you've run in the past
  * The most recently used backup template file is automatically loaded for you when starting the software. A backup is only a few clicks away!
* If copying a directory non-recursively, choose to only copy the last modified file in that directory. This could be useful when backing up a directory that contains local file backups, for example -- you may not need all the local backups, just the latest one.
* View backup progress as it happens
* Optional sounds when backup succeeds or fails
* Cancel backup at any time
* Incremental backups (only backups up latest files that have been changed; old files that are now gone are not deleted from the backup location)
* Windows legacy build only: Files for backup can be compressed with or without a password (uses 7-zip LZMA compression). By default, files are simply copied from their source directory to the backup location.

EasyBackup isn't the most fancy backup software out there, but it works well enough for simple backups. 

**USE AT YOUR OWN RISK. YOU MAY WANT TO VERIFY YOUR BACKUP MANUALLY AFTER COMPLETION. THE AUTHORS OF THIS SOFTWARE TAKE NO RESPONSBILITY IF SOMETHING FAILS.**

## Note on Unix platforms for version 0.10.0+

For now, you may need to run `chmod +x` on the download in macOS/Linux in order to get things running properly. In the future, we'd like to publish an actual macOS app.

## Screenshots (Legacy Windows-only version)

_The newer, cross-platform software looks very similar, so these screenshots should give you a good idea of how things work._

<div align="center">
    <img alt="Setup" src="./screenshots/setup-backup.png">
    <img alt="Backing up data" src="./screenshots/backing-up.png">
</div>

## Can I help contribute?

Glad you asked! There are always things that can be done on an open-source project: fix bugs, add new features, and more! Check out the issues tab of this repository and take a look at what bugs have been reported and which features have been requested. If you'd like to request a feature or file a bug, by all means, please do so!

Basically, this repo is not actively developed unless the author needs something for it, but contributions are welcome and accepted.

## License

The core EasyBackup code is under the MIT License. Thanks for using this software!

Compressed files are created using [7-Zip](https://www.7-zip.org) (7za.exe). 7-Zip is licensed under the GNU LGPL license. You can find the source code for 7-Zip at [www.7-zip.org](https://www.7-zip.org).
