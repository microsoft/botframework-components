# Components YAML pipelines
Each component in this repository requires a YAML pipeline to define the packaging process that enables publishing on both NuGet and NPM feeds.

## Getting started
When a component is ready to be packaged for testing, use the following instructions:
1. Duplicate the [starter template](/templates/startTemplate.yml) into the [pipelines directory](/pipelines) and give it a unique name.
1. Replace the `{YOUR_WORKING_DIRECTORY}` references with the working directory of your component. For example, "/packages/foo".
1. Replace {COMPONENT_TYPE} with the type of package your component is.
  - declarativeAsset (_Only consists of exported dialog, lg, lu, and/or qna files_)
  - codeExtension (_Has code for an adapter, custom action, middleware, recognizer, etc._)
  - generator (_Yeoman generators for bot templates_)

## Navigation
### [Pipelines](/pipelines)
Contains the pipelines that Azure DevOps will reference.

### [Tasks](/tasks)
Contains individual pipeline steps for reuse.

### [Templates directory](/templates)
Contains the templates that all pipelines should be built from.