using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Templates;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Constants = Microsoft.Recognizers.Text.DataTypes.TimexExpression.Constants;

namespace Microsoft.Bot.Solutions.Extensions.Input
{
    public class EventDateTimeInput : InputDialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Calendar.EventDateTimeInput";

        protected const string _dateProperty = "this.date";
        protected const string _timeProperty = "this.time";
        protected const string _durationProperty = "this.duration";

        [JsonProperty("timeZoneProperty")]
        public StringExpression TimeZoneProperty { get; set; }

        public EventDateTimeInput([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
        {
            RegisterSourceLocation(callerPath, callerLine);
        }

        public EventDateTimeInput()
        {
            MaxTurnCount = null;
        }

        public string DefaultLocale { get; set; } = null;

        protected override async Task<InputState> OnRecognizeInputAsync(DialogContext dc, CancellationToken cancellationToken)
        {
            var dcState = dc.State;
            var input = dcState.GetValue<object>(VALUE_PROPERTY);
            var timeZoneProperty = this.TimeZoneProperty.GetValue(dcState);
            var culture = GetCulture(dc);
            var refTime = dc.Context.Activity.LocalTimestamp?.LocalDateTime;
            var results = DateTimeRecognizer.RecognizeDateTime(input.ToString(), culture, refTime: refTime);
            DateTime? start = null;
            DateTime? end = null;
            var choices = new List<Choice>();

            if (results.Count > 0)
            {
                foreach (var res in results)
                {
                    var resolutionValues = (List<Dictionary<string, string>>)res.Resolution["values"];
                    var type = resolutionValues[0]["type"];
                    var dateTimeNowTZ = TimeZoneInfo.ConvertTime(DateTime.Today, TimeZoneInfo.FindSystemTimeZoneById(timeZoneProperty));

                    if (type == Constants.TimexTypes.DateTimeRange)
                    {
                        var validResolutions = resolutionValues.Where(r => DateTime.Parse(r["start"]) >= dateTimeNowTZ).ToList();
                        if (validResolutions.Count == 1)
                        {
                            start = DateTime.Parse(validResolutions[0]["start"]);
                            end = DateTime.Parse(validResolutions[0]["end"]);
                        }
                        else
                        {
                            foreach (var value in validResolutions)
                            {
                                var rangeStart = DateTime.Parse(value["start"]);
                                var rangeEnd = DateTime.Parse(value["end"]);

                                if (rangeStart.Date == rangeEnd.Date)
                                {
                                    choices.Add(new Choice($"{rangeStart:MMMM d, yyyy} {rangeStart:h:mmt} - {rangeEnd:h:mmt}"));
                                }
                                else
                                {
                                    choices.Add(new Choice($"{rangeStart:MMMM d, yyyy} {rangeStart:h:mmt} - {rangeEnd:MMMM d, yyyy} {rangeEnd:h:mmt}"));
                                }
                            }
                        }
                    }
                    else if (type == Constants.TimexTypes.DateRange)
                    {
                        var validResolutions = resolutionValues.Where(r => DateTime.Parse(r["start"]) >= dateTimeNowTZ).ToList();
                        if (validResolutions.Count == 1)
                        {
                            start = DateTime.Parse(validResolutions[0]["start"]);
                            end = DateTime.Parse(validResolutions[0]["end"]);
                        }
                        else
                        {
                            foreach (var value in validResolutions)
                            {
                                var rangeStart = DateTime.Parse(value["start"]);
                                var rangeEnd = DateTime.Parse(value["end"]);
                                choices.Add(new Choice($"{rangeStart:MMMM d, yyyy} - {rangeEnd:MMMM d, yyyy}"));
                            }
                        }
                    }
                    else if (type == Constants.TimexTypes.TimeRange)
                    {
                        if (resolutionValues.Count == 1)
                        {
                            var rangeStart = TimeSpan.Parse(resolutionValues[0]["start"]);
                            var rangeEnd = TimeSpan.Parse(resolutionValues[0]["end"]);

                            start = dateTimeNowTZ.Add(rangeStart);
                            end = dateTimeNowTZ.Add(rangeEnd);
                        }
                        else
                        {
                            foreach (var value in resolutionValues)
                            {
                                var rangeStart = DateTime.Parse(value["start"]);
                                var rangeEnd = DateTime.Parse(value["end"]);
                                choices.Add(new Choice($"{rangeStart:h:mmt} - {rangeEnd:h:mmt}"));
                            }
                        }
                    }
                    else if (type == Constants.TimexTypes.DateTime)
                    {
                        var validResolutions = resolutionValues.Where(r => DateTime.Parse(r["value"]) >= dateTimeNowTZ).ToList();
                        if (validResolutions.Count == 1)
                        {
                            var dateTime = DateTime.Parse(validResolutions[0]["value"]);
                            dcState.SetValue(_dateProperty, dateTime.Date);
                            dcState.SetValue(_timeProperty, dateTime.TimeOfDay);
                        }
                        else
                        {
                            foreach (var value in validResolutions)
                            {
                                var date = DateTime.Parse(value["value"]);
                                choices.Add(new Choice($"{date:dddd MMMM d, yyyy} at {date.TimeOfDay:h:mmt}"));
                            }
                        }
                    }
                    else if (type == Constants.TimexTypes.Date)
                    {
                        var validResolutions = resolutionValues.Where(r => DateTime.Parse(r["value"]) >= dateTimeNowTZ).ToList();
                        if (validResolutions.Count == 1)
                        {
                            var date = DateTime.Parse(validResolutions[0]["value"]);
                            dcState.SetValue(_dateProperty, date);
                        }
                        else
                        {
                            foreach (var value in validResolutions)
                            {
                                var date = DateTime.Parse(value["value"]);
                                choices.Add(new Choice(date.ToString("MMMM d, yyyy")));
                            }
                        }
                    }
                    else if (type == Constants.TimexTypes.Time)
                    {
                        if (resolutionValues.Count == 1)
                        {
                            var time = TimeSpan.Parse(resolutionValues[0]["value"]);
                            var date = dcState.GetValue<DateTime?>(_dateProperty);
                            var duration = dcState.GetValue<int?>(_durationProperty);
                            if (date != null)
                            {
                                if (duration != null)
                                {
                                    start = date.Value.Date
                                        .AddHours(time.Hours)
                                        .AddMinutes(time.Minutes)
                                        .AddSeconds(time.Seconds);
                                    end = start.Value.AddSeconds(duration.Value);
                                }
                                else
                                {
                                    dcState.SetValue(_timeProperty, new TimeSpan(time.Hours, time.Minutes, time.Seconds));
                                }
                            }
                            else
                            {
                                if (duration != null)
                                {
                                    start = dateTimeNowTZ.AddHours(time.Hours).AddMinutes(time.Minutes).AddSeconds(time.Seconds);
                                    end = start.Value.AddSeconds(duration.Value);
                                }
                                else
                                {
                                    dcState.SetValue(_dateProperty, dateTimeNowTZ);
                                    dcState.SetValue(_timeProperty, new TimeSpan(time.Hours, time.Minutes, time.Seconds));
                                }
                            }
                        }
                        else
                        {
                            foreach (var value in resolutionValues)
                            {
                                var time = DateTime.Parse(value["value"]).ToString("h:mmt");
                                choices.Add(new Choice(time));
                            }
                        }
                    }
                    else if (type == Constants.TimexTypes.Duration)
                    {
                        var durationInSeconds = int.Parse(resolutionValues[0]["value"]);
                        var date = dcState.GetValue<DateTime?>(_dateProperty);
                        var time = dcState.GetValue<TimeSpan?>(_timeProperty);

                        if (date != null && time != null)
                        {
                            start = date.Value.Add(time.Value);
                            end = start.Value.AddSeconds(durationInSeconds);
                        }
                        else
                        {
                            dcState.SetValue(_durationProperty, durationInSeconds);
                        }
                    }
                }

                if (start != null && end != null)
                {
                    dynamic result = new JObject();
                    result.start = start;
                    result.end = end;
                    dcState.SetValue(VALUE_PROPERTY, result);
                    return InputState.Valid;
                }
                else
                {
                    // ask follow up questions
                    var date = dcState.GetValue<DateTime?>(_dateProperty);
                    var time = dcState.GetValue<TimeSpan?>(_timeProperty);
                    var duration = dcState.GetValue<int?>(_durationProperty);

                    if (choices.Any())
                    {
                        InvalidPrompt = new StaticActivityTemplate((Activity)ChoiceFactory.HeroCard(choices, "Which one?"));
                    }
                    else if (date == null)
                    {
                        InvalidPrompt = new ActivityTemplate("What date?");
                    }
                    else if (time == null)
                    {
                        InvalidPrompt = new ActivityTemplate("What time?");
                    }
                    else if (duration == null)
                    {
                        InvalidPrompt = new ActivityTemplate("How long should it be?");
                    }

                    return InputState.Invalid;
                }
            }
            else
            {
                return InputState.Unrecognized;
            }
        }

        private string GetCulture(DialogContext dc)
        {
            if (!string.IsNullOrEmpty(dc.Context.Activity.Locale))
            {
                return dc.Context.Activity.Locale;
            }

            if (!string.IsNullOrEmpty(DefaultLocale))
            {
                return DefaultLocale;
            }

            return Culture.English;
        }
    }
}
