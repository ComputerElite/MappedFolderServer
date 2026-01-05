# MappedFolderServer
A server made primarily to serve reveal.js presentations.

## Features
- File webserver with access control
- Folders have slugs for access
- Slugs are guids by default, providing secure names against guessing
- Slugs can be made private, public or password protected
- As a user you can allow other devices to access a slug by inputting the other devices id from the main page
- The admin user can decide which folders on the server other users are allowed to access
- Can generate html files for putting on an usb stick which then opens a (private) presentation
- Allows to download a slug if you had access so you have an offline copy. This optionally includes content from remote webservers.

## Objective
I want to hold presentation with reveal.js. However, I do not want to rely on my laptop as a presentation device as we have 'Digitale Tafeln' (Basically 75" 4k Android Tablets).

Therefore, I want to host my presentations online so I can remotely control them via my phone, but still open them securely without revealing any credentials (e.g. open file on usb stick, but no password input)

Presentations shouldn't be accessed by third parties unless I explicitly want to. But I still want to manage them via a coherent file structure.

## Recommended action after setup
Add the [MappedFolderServer-public](https://github.com/ComputerElite/MappedFolderServer-public) repo as slug under `/public/` and mark them as public. This way many files required for Reveal.js will be present for all presentations to access centrally.

For local usage of Reveal.js I use following setup:

```
- ~/Documents/presentations/:
    |-- public (repo from https://github.com/ComputerElite/MappedFolderServer-public) 
    |-- presentation1
    |    |-- index.html (presentation 1 including all relevant files from /public)
    |-- presentation2
         |-- index.html (presentation 1 including all relevant files from /public)
         |-- assets
              |-- pic1.webp
```

Then using Live Server or Five server within vscodium I open the presentations folder to check my presentations.

The presentations are synced using syncthing to my mfs host

## Deploy using docker/podman compose
I provide 2 compose files for usage:
- `compose.example.yaml`: Allows deployment via prebuilt images
- `compose.build.yaml`: Allows deployment by compiling directly from source (needs repo and submodules cloned)

### Quick Deploy
Run following command to download all required files:

```bash
wget https://github.com/ComputerElite/MappedFolderServer/raw/refs/heads/main/compose.example.yaml && mv compose.example.yaml compose.yaml && wget https://github.com/ComputerElite/MappedFolderServer/raw/refs/heads/main/.env.example && mv .env.example .env
```

Afterwards modify `.env` to your needs and deploy using `docker compose up` or `podman compose up`

### Build from source
Clone the repo:
```bash
git clone https://github.com/ComputerElite/MappedFolderServer.git && cd MappedFolderServer && git submodule update --init && cp compose.build.yaml compose.yaml && cp .env.example .env
```

Modify `.env` and `compose.yaml` and build your images from source using `docker compose up` or `podman compose up`