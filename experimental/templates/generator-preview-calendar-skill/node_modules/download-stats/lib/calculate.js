'use strict';

var utils = require('./utils');

var calculate = module.exports = {};

/**
 * Group array into object where keys are groups and values are arrays.
 * Groups determined by provided `fn`.
 *
 * ```js
 * var groups = calculate.group(downloads, function(download) {
 *   // day is formatted as '2010-12-25'
 *   // add this download to the '2010-12' group
 *   return download.day.substr(0, 7);
 * });
 * ```
 * @param  {Array} `arr` Array of download objects
 * @param  {Function} `fn` Function to determine group the download belongs in.
 * @return {String} Key to use for the group
 * @api public
 */

calculate.group = function(arr, fn) {
  var groups = {};
  var len = arr.length, i = 0;
  while (len--) {
    var download = arr[i++];
    var groupArr = utils.arrayify(fn(download));
    groupArr.reduce(function(acc, group) {
      if (typeof group === 'string') {
        group = { name: group };
      }
      acc[group.name] = acc[group.name] || group;
      acc[group.name].downloads = acc[group.name].downloads || [];
      acc[group.name].downloads.push(download);
      return acc;
    }, groups);
  }
  return groups;
};

/**
 * Calculate the total for each group (key) in the object.
 *
 * @name group.total
 * @param  {Object} `groups` Object created by a `group` function.
 * @return {Object} Object with calculated totals
 * @api public
 */

calculate.group.total = function(groups) {
  var res = {};
  var keys = Object.keys(groups);
  var len = keys.length, i = 0;
  while (len--) {
    var key = keys[i++];
    var group = groups[key];
    if (Array.isArray(group)) {
      res[key] = calculate.total(group);
    } else {
      res[key] = calculate.total(group.downloads);
    }
  }
  return res;
};

/**
 * Calculate the total downloads for an array of download objects.
 *
 * @param  {Array} `arr` Array of download objects (must have a `.downloads` property)
 * @return {Number} Total of all downloads in the array.
 * @api public
 */

calculate.total = function(arr) {
  arr = utils.arrayify(arr);
  var len = arr.length, i = 0;
  var total = 0;
  while (len--) total += arr[i++].downloads || 0;
  return total;
};

/**
 * Calculate the average for each group (key) in the object.
 *
 * @name group.avg
 * @param  {Object} `groups` Object created by a `group` function.
 * @return {Object} Object with calculated average
 * @api public
 */

calculate.group.avg = function(groups, days) {
  var res = {};
  var keys = Object.keys(groups);
  var len = keys.length, i = 0;
  while (len--) {
    var key = keys[i++];
    res[key] = calculate.avg(groups[key], days);
  }
  return res;
};

/**
 * Calculate the average downloads for an array of download objects.
 *
 * @param  {Array} `arr` Array of download objects (must have a `.downloads` property)
 * @return {Number} Average of all downloads in the array.
 * @api public
 */

calculate.avg = function(arr, days) {
  arr = utils.arrayify(arr);
  var len = arr.length, i = 0;
  var total = 0;
  while (len--) {
    total += arr[i++].downloads || 0;
  }

  if (typeof days === 'undefined' || days === 0) {
    days = arr.length;
  }
  return total / days;
};

/**
 * Create an array of downloads before specified day.
 *
 * @name group.before
 * @param  {String} `day` Day specifying last day to use in group.
 * @param  {Array} `arr` Array of downloads to check.
 * @return {Array} Array of downloads happened before or on specified day.
 * @api public
 */

calculate.group.before = function(day, arr) {
  var end = utils.format(normalizeDate(utils.moment(day)));
  var group = [];
  var len = arr.length, i = 0;
  while (len--) {
    var download = arr[i++];
    if (download.day <= end) {
      group.push(download);
    }
  }
  return group;
};

/**
 * Calculate the total downloads happening before the specified day.
 *
 * @param  {String} `day` Day specifying last day to use in group.
 * @param  {Array} `arr` Array of downloads to check.
 * @return {Number} Total downloads happening before or on specified day.
 * @api public
 */

calculate.before = function(day, arr) {
  var group = calculate.group.before(day, arr);
  return calculate.total(group);
};

/**
 * Create an array of downloads for the last `X` days.
 *
 * @name group.last
 * @param  {Number} `days` Number of days to go back.
 * @param  {Array} `arr` Array of downloads to check.
 * @param {String} `init` Optional day to use as the last day to include. (Days from `init || today` - `days` to `init || today`)
 * @return {Array} Array of downloads for last `X` days.
 * @api public
 */

calculate.group.last = function(days, arr, init) {
  var today = init ? utils.moment.utc(init) : utils.moment.utc();
  var start = utils.moment.utc(today);
  start.subtract(days, 'days')
  today = utils.format(today);
  start = utils.format(start);

  var group = [];
  var len = arr.length, i = 0;
  while (len--) {
    var download = arr[i++];
    if (download.day > start && download.day <= today) {
      group.push(download);
    }
  }
  return group;
};

/**
 * Calculate total downloads for the last `X` days.
 *
 * @name last
 * @param  {Number} `days` Number of days to go back.
 * @param  {Array} `arr` Array of downloads to check.
 * @param {String} `init` Optional day to use as the last day to include. (Days from `init || today` - `days` to `init || today`)
 * @return {Array} Array of downloads for last `X` days.
 * @api public
 */

calculate.last = function(days, arr, init) {
  var group = calculate.group.last(days, arr, init);
  return calculate.total(group);
};

/**
 * Create an array of downloads for the previous `X` days.
 *
 * @name group.prev
 * @param  {Number} `days` Number of days to go back.
 * @param  {Array} `arr` Array of downloads to check.
 * @param {String} `init` Optional day to use as the prev day to include. (Days from `init || today` - `days` - `days` to `init || today` - `days`)
 * @return {Array} Array of downloads for prev `X` days.
 * @api public
 */

calculate.group.prev = function(days, arr, init) {
  var today = init ? utils.moment(init) : utils.moment();
  var end = utils.moment(today);
  end.subtract(days, 'days');
  return calculate.group.last(days, arr, end);
};

/**
 * Calculate total downloads for the previous `X` days.
 *
 * @name prev
 * @param  {Number} `days` Number of days to go back.
 * @param  {Array} `arr` Array of downloads to check.
 * @param {String} `init` Optional day to use as the prev day to include. (Days from `init || today` - `days` - `days` to `init || today` - `days`)
 * @return {Array} Array of downloads for prev `X` days.
 * @api public
 */

calculate.prev = function(days, arr, init) {
  var group = calculate.group.prev(days, arr, init);
  return calculate.total(group);
};

/**
 * Create an object of download groups by month.
 *
 * @param  {Array} `arr` Array of downloads to group and total.
 * @return {Object} Groups with arrays of download objects
 * @api public
 */

calculate.group.monthly = function(arr) {
  return calculate.group(arr, function(download) {
    return download.day.substr(0, 7);
  });
};

function normalizeDate(date) {
  date.utc().hour(0);
  date.utc().minute(0);
  date.utc().second(0);
  return date;
}

calculate.group.window = function(days, arr, init) {
  var today = init ? utils.moment(init) : normalizeDate(utils.moment());
  arr = calculate.group.before(today, arr);
  return calculate.group(arr, function(download) {
    var day = utils.moment.utc(download.day);
    var diff = day.diff(today, 'days');
    var period = Math.floor((diff * -1) / days);
    var start = utils.moment(today);
    start.subtract((period + 1) * days, 'days');
    return {
      name: period,
      period: utils.format(start)
    };
  });
};

/**
 * Calculate total downloads grouped by month.
 *
 * @param  {Array} `arr` Array of downloads to group and total.
 * @return {Object} Groups with total downloads calculated
 * @api public
 */

calculate.monthly = function(arr) {
  var months = calculate.group.monthly(arr);
  return calculate.group.total(months);
};

/**
 * Create an object of download groups by month.
 *
 * @param  {Array} `arr` Array of downloads to group and total.
 * @return {Object} Groups with arrays of download objects
 * @api public
 */
calculate.group.yearly = function(arr) {
  return calculate.group(arr, function(download) {
    return download.day.substr(0, 4);
  });
};

/**
 * Calculate total downloads grouped by year.
 *
 * @param  {Array} `arr` Array of downloads to group and total.
 * @return {Object} Groups with total downloads calculated
 * @api public
 */

calculate.yearly = function(arr) {
  var years = calculate.group.yearly(arr);
  return calculate.group.total(years);
};
