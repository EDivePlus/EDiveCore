#if XR_TOOLKIT
using EDIVE.GeoToolkit.Maps;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace EDIVE.GeoToolkit.XRTools
{
    public class MapTeleportationArea : TeleportationArea
    {
        [Required]
        [SerializeField]
        private MapController _MiniMap;

        [Required]
        [SerializeField]
        private MapController _TerrainMap;

        protected override bool GenerateTeleportRequest(IXRInteractor interactor, RaycastHit hit, ref TeleportRequest teleportRequest)
        {
            if (hit.collider == null)
                return false;

            if (IsSphereCastRay(interactor, out _) && IsSphereCastOverlap(hit))
                return false;

            var coordinates = _MiniMap.ConvertToGeoCoordinates(hit.point);
            var worldPos = _TerrainMap.ConvertToMapCoordinates(coordinates, true);

            teleportRequest.destinationPosition = worldPos;
            teleportRequest.destinationRotation = transform.rotation;
            teleportRequest.matchOrientation = MatchOrientation.WorldSpaceUp;
            return true;
        }

        private static bool IsSphereCastRay(IXRInteractor interactor, out XRRayInteractor rayInteractor)
        {
            rayInteractor = interactor as XRRayInteractor;
            return rayInteractor != null && rayInteractor.hitDetectionType == XRRayInteractor.HitDetectionType.SphereCast;
        }

        private static bool IsSphereCastOverlap(RaycastHit raycastHit)
        {
            if (raycastHit.distance != 0f)
                return false;

            var point = raycastHit.point;
            return point.x == 0f && point.y == 0f && point.z == 0f;
        }
    }
}
#endif
