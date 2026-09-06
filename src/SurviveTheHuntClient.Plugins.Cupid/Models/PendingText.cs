using SurviveTheHuntClient.Plugins.Cupid.Helpers;

namespace SurviveTheHuntClient.Plugins.Cupid.Models
{
    internal class PendingText
    {
        internal PhoneContactInfo Sender;
        internal string Subject;
        internal string Message;
        internal float Duration;
        internal float RemainingTime;

        internal readonly byte Tag;

        internal PendingText(float delay, PhoneContactInfo sender, string subject, string message, float duration = PhoneTextHelper.DefaultMessageDurationSeconds, byte tag = 0)
        {
            Sender = sender;
            Subject = subject;
            Message = message;
            Duration = duration;
            RemainingTime = delay;
            Tag = tag;
        }
    }
}
