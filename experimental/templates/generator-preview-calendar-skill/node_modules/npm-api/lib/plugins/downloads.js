'use strict';

const utils = require('../utils');
const define = (obj, name, value) => Reflect.defineProperty(obj, name, { value });

module.exports = (options = {}) => {
  return function() {
    let log = (...args) => {
      if (this.options.verbose === true || options.verbose === true) {
        console.log(...args);
      }
    };

    define(this, 'total', async() => {
      if (!this.cache.has('total')) {
        let results = await this.downloads();
        this.cache.set('total', utils.stats.calc.total(results));
      }
      return this.cache.get('total');
    });

    define(this, 'last', async(n) => {
      let key = 'last-' + n;
      if (!this.cache.has(key)) {
        let results = await this.downloads();
        this.cache.set(key, utils.stats.calc.last(n, results));
      }
      return this.cache.get(key);
    });

    define(this, 'downloads', (start = '2005-01-01') => {
      let end = this.options.end
        ? utils.moment(this.options.end)
        : utils.moment.utc().subtract(1, 'days');

      start = utils.moment(start);
      let downloads = [];
      return new Promise((resolve, reject) => {
        log('getting downloads for "' + this.name + '"');
        utils.stats.get(start, end, this.name)
          .on('data', (data) => {
            downloads.push(data);
          })
          .on('error', (err) => {
            log('ERROR: [' + this.name + ']');
            log(err);
          })
          .on('end', () => {
            downloads.sort((a, b) => {
              if (a.day < b.day) return 1;
              if (a.day > b.day) return -1;
              return 0;
            });
            let results = [];
            downloads.forEach(download => {
              if (!results.find(d => d.day === download.day)) {
                results.push(download);
              }
            });
            resolve(results);
          });
      });
    });
  };
};
