using CanteenRush.Core;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
namespace CanteenRush.Gameplay
{
    internal static class HandResolver
    {
        public static Hand Resolve(IXRInteractor interactor) => interactor != null && interactor.handedness == InteractorHandedness.Left ? Hand.Left : Hand.Right;
    }
}
