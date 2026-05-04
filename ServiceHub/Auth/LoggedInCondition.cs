// Author: Michal Petr
// Created: 04.05.2026

using EDIVE.Conditions;

namespace EDIVE.ServiceHub.Auth
{
    public class LoggedInCondition : ICondition
    {
        public bool Evaluate()
        {
            return AuthStorage.Client.IsValid();
        }
    }
}
