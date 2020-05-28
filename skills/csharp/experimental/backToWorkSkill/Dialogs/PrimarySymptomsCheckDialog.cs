using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BackToWorkSkill.Dialogs
{
    public class PrimarySymptomsCheckDialog : BackToWorkSkillDialogBase
    {
        public PrimarySymptomsCheckDialog(
            IServiceProvider serviceProvider)
            : base(nameof(PrimarySymptomsCheckDialog), serviceProvider)
        {
            var checkSymptoms = new WaterfallStep[]
            {
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                GetPrimarySymptomsAsync
            };

            AddDialog(new WaterfallDialog(Actions.GetPrimarySymptoms, checkSymptoms));
        }

        protected async Task<DialogTurnResult> GetPrimarySymptomsAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await sc.EndDialogAsync(await CreateActionResultAsync(sc.Context, true, cancellationToken), cancellationToken);
        }
    }
}
