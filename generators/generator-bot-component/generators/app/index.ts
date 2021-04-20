import * as t from 'runtypes';
import Generator from 'yeoman-generator';

enum Language {
  dotnet = 'dotnet',
  js = 'js',
}

export default class extends Generator {
  constructor(args: string | string[], options: Record<string, unknown>) {
    super(args, options);

    this.argument('componentName', {
      required: true,
      type: String,
    });

    this.option('language', {
      default: 'dotnet',
      type: String,
      description: `Package language, one of ["${Object.keys(Language).join(
        '", "'
      )}"]`,
    });
  }

  generate(): void {
    const { componentName, language } = t
      .Record({
        componentName: t.String,
        language: t.Guard<Language>(
          (val): val is Language => Language[val as Language] !== undefined,
          {
            name: 'Language',
          }
        ),
      })
      .check(this.options);

    const context = { componentName };

    this.fs.copyTpl(
      this.templatePath('common'),
      this.destinationPath(componentName),
      context
    );

    this.fs.copyTpl(
      this.templatePath(language),
      this.destinationPath(componentName),
      context,
      {},
      {
        globOptions: { dot: true },
      }
    );

    switch (language) {
      case Language.dotnet: {
        this.fs.move(
          this.destinationPath(componentName, 'Project.csproj'),
          this.destinationPath(componentName, `${componentName}.csproj`)
        );

        this.fs.move(
          this.destinationPath(componentName, 'Component.cs'),
          this.destinationPath(componentName, `${componentName}.cs`)
        );

        return;
      }

      case Language.js: {
        return;
      }
    }
  }
}
