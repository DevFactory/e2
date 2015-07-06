# E2

## Build Guide

E2 is written in F#, a functional programming language that primarily runs on .NET runtime. Mono is an open-source 
implementation of .NET. Building E2 on Mono is recommended, although Visual Studio is also supported.

### Prerequisites

E2 has several dependencies.

- [Mono](http://www.mono-project.com/) and F#
- [Paket](https://fsprojects.github.io/Paket/)
- [FParsec](http://www.quanttec.com/fparsec/)

Install Mono and F#:

    brew install fsharp

Under the project directory:

    wget https://github.com/fsprojects/Paket/releases/download/1.18.5/paket.exe
    mono paket.exe install
    
### Compile

Under the project directory, run:

    xbuild /t:clean
    xbuild
    
The main assembly will be generated as `build/e2.exe`. Run `mono build/e2.exe` to execute E2.
