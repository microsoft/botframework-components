// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Controllers
{
    // This ASP Controller is created to handle a request. Dependency Injection will provide the Adapter and IBot
    // implementation at runtime. Multiple different IBot implementations running at different endpoints can be
    // achieved by specifying a more specific type for the bot constructor argument.
    [ApiController]
    public class CardsController : ControllerBase
    {
        private static readonly string Music = "music.mp3";

        [Route("api/music")]
        [HttpGet]
        public ActionResult ReturnFile()
        {
            var filename = Music;
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Dialogs/Cards/Files", filename);
            var fileData = System.IO.File.ReadAllBytes(filePath);

            return File(fileData, "audio/mp3");
        }
    }
}
