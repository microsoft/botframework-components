# Creating templates

Coming Soon!

## Docs table of contents

1. [Overview](/docs/overview.md)
2. [Extending your bot using packages](/docs/extending-with-packages.md)
3. [Extending your bot with code](/docs/extending-with-code.md)
4. [Creating your own packages](/docs/creating-packages.md)
5. [Creating your own templates](/docs/creating-templates.md)

## Template Publishing

Template packages indicate to Composer which azure environments and runtime languages it supports through the keywords associated with the package. 

The following feedUrl is used by Composer to grab the first party template generators.
- If user has not opted in to using preview generators
  - https://registry.npmjs.org/-/v1/search?text=generator+keywords:bf-template+scope:microsoft+maintainer:botframework

So any npm package in the microsoft scope, with botframework npm UN as a maintainer, with keyword bf-template will be returned.

Composer then parses through the keywords of each package to determine which scenarios the template supports. It uses this data when populating the 'node' and 'C#' tabs in the template selection view as well as the 'web app' and 'functions' options dropdown in the creation flow. The keywords are formatted as bf-{language}-{integration}


- bf-js-functions - Template supports js runtime within a functions env
- bf-js-webapp - template supports js runtime within a azure web app env
- bf-dotnet-webapp - template supports dotnet runtime within a webapp env
- bf-dotnet-functions - template supports a dotnet runtime within a functions env.

### Publishing preview/prod generators
In Composer there is the option to use preview generators. This allows template developers to publish preview/experimental versions of their template generators without disrupting the latest stable versions. 

To publish a preview generator:

[TODO]

Once you are comfortable to upgrade your preview generator to the latest stable generator 

[TODO] 

**NOTE: Upgrading latest stable generator will have immediate effect and all current instances of the Composer will now pull this version**