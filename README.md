# Easy Backup

Easy Backup is a small Windows utility that allows you to copy files from one location to another for the purposes of a backup. That's it, really. No file compression or file security involved -- just a simple "copy this thing to this other place" application for quick and easy backups to an external or other drive.

Features include:

* Choose files or folders to backup (either via file dialogs or via drag & drop)
* Toggle recursive copy of directories on and off
* Save and load your backup locations as a template so that it's easy as pie to re-run a backup that you've run in the past
  * The most recently used backup template file is automatically loaded for you when starting the software. A backup is only a few clicks away!
* If copying a directory non-recursively, choose to only copy the last modified file in that directory. This could be useful when backing up a directory that contains local file backups, for example -- you may not need all the local backups, just the latest one.
* View backup progress as it happens
* Cancel backup at any time

## Features/Updates to Come

* Option to update a previous backup instead of creating an entirely new backup (only adds new files and updates modified files based on a hash; does NOT delete old files that have been removed)

## Can I help contribute?

Glad you asked! There are always things that can be done on an open-source project: fix bugs, add new features, and more! Check out the issues tab of this repository and take a look at what bugs have been reported and which features have been requested. If you'd like to request a feature or file a bug, by all means, please do so!

## License

MIT License. Thanks for using this software!
