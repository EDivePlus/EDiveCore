// Author: Michal Petr
// Created: 03.11.2025

using EDIVE.Forms.Answers;
using EDIVE.Forms.Questions;

namespace EDIVE.Forms.Controllers
{
    public class LikertQuestionController : AFormQuestionController<LikertQuestion>
    {
        protected override void Initialize()
        {
            base.Initialize();
        }
        
        public override void Terminate()
        {
            base.Terminate();
        }

        public override void SetAnswer(AFormAnswer answer)
        {
            // TODO
        }
    }
}