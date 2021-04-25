# Command line instructions

Our templates can also be used from the command-line. First, install [Yeoman][yeoman] and @microsoft/generator-bot-empty using [npm][npm] (we assume you have pre-installed [node.js][nodejs]):

```bash
npm install -g yo
npm install -g @microsoft/generator-bot-empty
```

Then generate your new project:

```bash
yo @microsoft/generator-bot-empty -botname '{BOT_NAME}' -platform '{dotnet|js}' -integration '{functions|webapp}'
```

[yeoman]: https://yeoman.io
[npm]: https://npmjs.com
[nodejs]: https://nodejs.org/