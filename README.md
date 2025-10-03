# MappedFolderServer
A server made primarely to serve reveal.js presentations.

## Features
- File server with access control
- Folders have slugs for access
- Slugs are Guids by default, providing secure names against guessing
- Slugs can be made private, public or password protected
- As a user you can allow other devices to access a slug by inputting their id
- The admin user can decide which folders other users are allowed to access
- Can generate html files for putting on a usb stick which then opens a (private) presentation

## Objective
I have presentation I want to hold with reveal.js. However I do not want to rely on my laptop as a presentation device as we have 'Digitale Tafeln' (75" 4k Android Tablets).

Therefore I want to host my presentations online so I can also remote control them via my phone, but still open them with minimal afford (e. g. open file on usb stick)

Presentations shouldn't be accessed by third parties however unless I explicitly want to. But I still want to manage them via a coherent file structure.

