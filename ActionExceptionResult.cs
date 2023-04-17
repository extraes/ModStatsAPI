using Microsoft.AspNetCore.Mvc;

namespace ModStats
{
    public class ActionExceptionResult : ActionResult
    {
        public string message;
        public ActionExceptionResult(string message) 
        {
            this.message = message;
        }
        public ActionExceptionResult() : this(string.Empty) { }

        public override void ExecuteResult(ActionContext context)
        {
            throw new Exception(message);
        }
    }
}
