# E2

[![Build Status](https://travis-ci.org/changlan/e2.svg?branch=master)](https://travis-ci.org/changlan/e2)

E2 is written in F#, a functional programming language that primarily runs on .NET runtime. Mono is an open-source 
implementation of .NET. Building E2 on Mono is recommended, although Visual Studio is also supported.

### Dependencies

E2 has several dependencies.

- [Mono](http://www.mono-project.com/) and F#
- Nuget

### Build

Install Mono and F#:

    brew install fsharp

Under the project directory:

    nuget restore e2.sln
    xbuild /p:Configuration=Release e2.sln
    
The main assembly will be generated as `bin/e2.exe`. Run `mono bin/e2.exe` to execute E2.
