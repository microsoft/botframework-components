// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;

namespace MusicSkill.Tests.Utilities
{
    public class SkillTestUtilBase
    {
        public static MusicSkillLuis CreateIntent(
            string userInput,
            MusicSkillLuis.Intent intent,
            MusicSkillLuis._Entities.MusicParentClass[] musicParentClasses = null,
            string[] genres = null,
            string[][] volumeDirection = null)
        {
            var result = new MusicSkillLuis
            {
                Text = userInput,
                Intents = new Dictionary<MusicSkillLuis.Intent, IntentScore>()
            };

            result.Intents.Add(intent, new IntentScore() { Score = 0.9 });

            result.Entities = new MusicSkillLuis._Entities
            {
                _instance = new MusicSkillLuis._Entities._Instance(),
            };

            result.Entities.MusicParent = musicParentClasses;
            result.Entities.VolumeDirection = volumeDirection;
            result.Entities._instance.GenreList = GetInstanceDatas(userInput, genres);
            return result;
        }

        public static InstanceData[] GetInstanceDatas(string userInput, string[] entities)
        {
            if (userInput == null || entities == null)
            {
                return null;
            }

            var result = new InstanceData[entities.Length];
            for (int i = 0; i < entities.Length; i++)
            {
                var name = entities[i];
                var index = userInput.IndexOf(name);
                if (index == -1)
                {
                    throw new Exception("No such string in user input");
                }

                var instanceData = new InstanceData
                {
                    StartIndex = index,
                    EndIndex = index + name.Length,
                    Text = name
                };

                result[i] = instanceData;
            }

            return result;
        }
    }
}
