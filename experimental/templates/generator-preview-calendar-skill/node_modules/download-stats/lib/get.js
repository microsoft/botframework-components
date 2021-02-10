'use strict';
var utils = require('./utils');

/**
 * Get a range of download counts for the specified repository.
 * This method returns a stream of raw data
 * in the form of `{ day: '2016-01-10', downloads: 123456 }`.
 *
 * ```js
 * var start = new Date('2016-01-09');
 * var end = new Date('2016-01-10');
 * stats.get(start, end, 'micromatch')
 *   .on('error', console.error)
 *   .on('data', function(data) {
 *     console.log(data);
 *   })
 *   .on('end', function() {
 *     console.log('done.');
 *   });
 * // { day: '2016-01-09', downloads: 53331 }
 * // { day: '2016-01-10', downloads: 47341 }
 * ```
 *
 * @param  {Date} `start` Start date of stream.
 * @param  {Date} `end`   End date of stream.
 * @param  {String} `repo`  Repository to get downloads for. If `repo` is not passed, then all npm downloads for the day will be returned.
 * @return {Stream} Stream of download data.
 * @api public
 */

function get(start, end, repo) {
  start = utils.moment.utc(start);
  end = utils.moment.utc(end);
  var current = utils.moment.utc(start);
  var stream = new utils.stream.Stream();
  run();
  return stream;

  function run() {
    process.nextTick(function() {
      let period = utils.moment.utc(current).add(300, 'days');
      if (period.format('YYYY-MM-DD') >= end.format('YYYY-MM-DD')) {
        period = utils.moment.utc(end);
      }

      getPage(current, period, repo)
        .on('error', stream.emit.bind(stream, 'error'))
        .on('data', function(data) {
          stream.emit('data', data);
        })
        .on('end', function() {
          current.add(300, 'days');
          if (current.format('YYYY-MM-DD') >= end.format('YYYY-MM-DD')) {
            stream.emit('end');
            return;
          }
          run();
        });
    });
  }
}

function getPage(start, end, repo) {
  var stream = new utils.stream.Stream();
  var url = 'https://api.npmjs.org/downloads/range/';
  url += utils.format(start);
  url += ':' + utils.format(end);
  url += (repo ? '/' + repo : '');

  var bulk = false;
  if (repo && repo.indexOf(',') > -1) {
    bulk = true;
  }

  process.nextTick(function() {
    var req = utils.https.get(options(url), function(res) {
      res.on('error', console.error)
        .pipe(utils.JSONStream.parse(bulk ? '*' : 'downloads.*'))
        .on('error', handleError)
        .on('data', function(data) {
          stream.emit('data', data);
        })
        .on('end', stream.emit.bind(stream, 'end'));
    });

    req.on('error', stream.emit.bind(stream, 'error'));
  });

  return stream;

  function handleError(err) {
    console.error('handling error', err);
    if (err.message.indexOf('Invalid JSON') >= 0) {
      handleInvalidJSON();
      return;
    }
    stream.emit('error', err);
  }

  function handleInvalidJSON() {
    var body = '';
    utils.https.get(options(url), function(res) {
      res
        .on('error', stream.emit.bind('error'))
        .on('data', function(data) {
          body += data;
        })
        .on('end', function() {
          stream.emit('error', new Error(body));
        });
    });
  }
}

/**
 * Get a specific point (all-time, last-month, last-week, last-day)
 *
 * ```js
 * stats.get.period('last-day', 'micromatch', function(err, results) {
 *   if (err) return console.error(err);
 *   console.log(results);
 * });
 * // { day: '2016-01-10', downloads: 47341 }
 * ```
 * @param  {String} `period` Period to retrieve downloads for.
 * @param  {String} `repo` Repository to retrieve downloads for.
 * @param  {Function} `cb` Callback function to get results
 * @api public
 */

get.point = function(period, repo, cb) {
  var url = 'https://api.npmjs.org/downloads/point/';
  url += period;
  url += (repo ? '/' + repo : '');

  var results;
  var req = utils.https.get(options(url), function(res) {
    res.once('error', console.error)
      .pipe(utils.JSONStream.parse())
      .once('error', cb)
      .on('data', function(data) {
        results = data;
      })
      .once('end', function() {
        cb(null, results);
      });
  });

  req.once('error', cb);
};

/**
 * Get the all time total downloads for a repository.
 *
 * ```js
 * stats.get.allTime('micromatch', function(err, results) {
 *   if (err) return console.error(err);
 *   console.log(results);
 * });
 * // { day: '2016-01-10', downloads: 47341 }
 * ```
 * @param  {String} `repo` Repository to retrieve downloads for.
 * @param  {Function} `cb` Callback function to get results
 * @api public
 */

get.allTime = function(repo, cb) {
  return get.point('all-time', repo, cb);
};

/**
 * Get the last month's total downloads for a repository.
 *
 * ```js
 * stats.get.lastMonth('micromatch', function(err, results) {
 *   if (err) return console.error(err);
 *   console.log(results);
 * });
 * // { downloads: 7750788, start: '2016-10-10', end: '2016-11-08', package: 'micromatch' }
 * ```
 * @param  {String} `repo` Repository to retrieve downloads for.
 * @param  {Function} `cb` Callback function to get results
 * @api public
 */

get.lastMonth = function(repo, cb) {
  return get.point('last-month', repo, cb);
};

/**
 * Get the last week's total downloads for a repository.
 *
 * ```js
 * stats.get.lastWeek('micromatch', function(err, results) {
 *   if (err) return console.error(err);
 *   console.log(results);
 * });
 * // { downloads: 1777065, start: '2016-11-02', end: '2016-11-08', package: 'micromatch' }
 * ```
 * @param  {String} `repo` Repository to retrieve downloads for.
 * @param  {Function} `cb` Callback function to get results
 * @api public
 */

get.lastWeek = function(repo, cb) {
  return get.point('last-week', repo, cb);
};

/**
 * Get the last day's total downloads for a repository.
 *
 * ```js
 * stats.get.lastDay('micromatch', function(err, results) {
 *   if (err) return console.error(err);
 *   console.log(results);
 * });
 * // { downloads: 316004, start: '2016-11-08', end: '2016-11-08', package: 'micromatch' }
 * ```
 * @param  {String} `repo` Repository to retrieve downloads for.
 * @param  {Function} `cb` Callback function to get results
 * @api public
 */

get.lastDay = function(repo, cb) {
  return get.point('last-day', repo, cb);
};

function options(url) {
  var opts = utils.url.parse(url);
  opts.headers = {'User-Agent': 'https://github.com/doowb/download-stats'};
  return opts;
}

/**
 * Expose `get`
 */

module.exports = get;
