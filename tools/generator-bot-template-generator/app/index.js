'use strict';
const path = require('path');
const Generator = require('yeoman-generator');
const askName = require('inquirer-npm-name');
const _ = require('lodash');
const extend = require('deep-extend');
const mkdirp = require('mkdirp');

function makeGeneratorName(name) {
  name = _.kebabCase(name);
  name = name.indexOf('generator-') === 0 ? name : 'generator-' + name;
  return name;
}

module.exports = class extends Generator {
  initializing() {
    this.props = {};
  }

  prompting() {
    return askName(
      {
        name: 'name',
        message: 'Your generator name',
        default: makeGeneratorName(path.basename(process.cwd())),
        filter: makeGeneratorName,
        validate: str => {
          return str.length > 'generator-'.length;
        }
      },
      this
    ).then(props => {
      this.props.name = props.name;
    });
  }

  default() {
    if (path.basename(this.destinationPath()) !== this.props.name) {
      this.log(
        `Your generator must be inside a folder named ${
          this.props.name
        }\nI'll automatically create this folder.`
      );
      mkdirp(this.props.name);
      this.destinationRoot(this.destinationPath(this.props.name));
    }
  }

  _normalizeName = (generatorName) => {
    if (generatorName.indexOf('generator-') !== -1) {
      return generatorName.replace('generator-', '');
    } 
    return generatorName;
  }

  _changeFileNames = (generatorName) => {
    const normalizedName = this._normalizeName(generatorName);
    console.log(this.destinationPath(path.join('generators', 'app', 'templates', 'knowledge-base', 'en-us', 'emptyBot.en-us.qna')));
    this.fs.move(
      this.destinationPath(path.join('generators', 'app', 'templates', 'knowledge-base', 'en-us', 'emptyBot.en-us.qna')),
      this.destinationPath(path.join('generators', 'app', 'templates', 'knowledge-base', 'en-us', `${normalizedName}.en-us.qna`))
  );
  }

  writing() {
    this.fs.copyTpl(
      this.templatePath(),
      this.destinationPath(),
      { name: this.props.name }
    );

  }
  // end() {
  //   console.log(this.destinationPath());
  //   this._changeFileNames(this.props.name);
  // };
};

