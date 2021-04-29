# Components YAML pipelines
Each component in this repository requires a YAML pipeline to define the packaging process that enables publishing on both NuGet and NPM feeds.

## Getting started
When a component is ready to be packaged for testing, use the following instructions:
1. Duplicate the [starter template](/pipelines/starter-pipeline.yml) in the [pipelines directory](/pipelines) and give it a unique name.
1. Replace `{YOUR_COMPONENT_TYPE}` with the type of package your component is.
  - declarativeAsset (_Only consists of exported dialog, lg, lu, and/or qna files. Packages for nuget and npm feeds._)
  - codeExtension (_Has code for an adapter, custom action, middleware, recognizer, etc. Packages for nuget feeds (at this time)._)
  - generator (_Yeoman generators for bot templates. Packages for npm feeds._)
1. Replace `{YOUR_DEPLOYMENT_RING}` with:
  - alpha (_Package is in private preview_)
  - preview (_Package is in public preview_)
  - stable (_Package is ready for release_)
1. Replace the `{YOUR_WORKING_DIRECTORY}` references with the working directory of your component. For example, "/packages/foo".

## Navigation
### [Pipelines](/pipelines)
Contains the pipelines that Azure DevOps will reference.

### [Templates directory](/templates)
Contains the templates that all pipelines should be built from.
