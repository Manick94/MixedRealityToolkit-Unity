﻿
using Microsoft.MixedReality.Toolkit.UI.Experimental.BoundsControlTypes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Microsoft.MixedReality.Toolkit.UI.Experimental
{
    /// <summary>
    /// Links that are rendered in between the corners of <see cref="BoundsControl"/>
    /// </summary>
    [CreateAssetMenu(fileName = "BoundsControlLinks", menuName = "Mixed Reality Toolkit/Bounds Control/Links")]
    public class BoundsControlLinks : ScriptableObject
    {
        #region Serialized Properties
        [SerializeField]
        [Tooltip("Material used for wireframe display")]
        private Material wireframeMaterial;

        /// <summary>
        /// Material used for wireframe display
        /// </summary>
        public Material WireframeMaterial
        {
            get { return wireframeMaterial; }
            set
            {
                if (wireframeMaterial != value)
                {
                    wireframeMaterial = value;
                    SetMaterials();
                    configurationChanged.Invoke();
                }
            }
        }

        [SerializeField]
        [Tooltip("Radius for wireframe edges")]
        private float wireframeEdgeRadius = 0.001f;

        /// <summary>
        /// Radius for wireframe edges
        /// </summary>
        public float WireframeEdgeRadius
        {
            get { return wireframeEdgeRadius; }
            set
            {
                if (wireframeEdgeRadius != value)
                {
                    wireframeEdgeRadius = value;
                    configurationChanged.Invoke();
                }
            }
        }

        [SerializeField]
        [Tooltip("Shape used for wireframe display")]
        private WireframeType wireframeShape = WireframeType.Cubic;

        /// <summary>
        /// Shape used for wireframe display
        /// </summary>
        public WireframeType WireframeShape
        {
            get { return wireframeShape; }
            set
            {
                if (wireframeShape != value)
                {
                    wireframeShape = value;
                    configurationChanged.Invoke();
                }
            }
        }

        [SerializeField]
        [Tooltip("Show a wireframe around the bounds control when checked. Wireframe parameters below have no effect unless this is checked")]
        private bool showWireframe = true;

        /// <summary>
        /// Show a wireframe around the bounds control when checked. Wireframe parameters below have no effect unless this is checked
        /// </summary>
        public bool ShowWireFrame
        {
            get { return showWireframe; }
            set
            {
                if (showWireframe != value)
                {
                    showWireframe = value;
                    configurationChanged.Invoke();
                }
            }
        }

        #endregion Serialized Properties

        internal protected UnityEvent configurationChanged = new UnityEvent();

        /// <summary>
        /// defines a bounds control link
        /// </summary>
        private class Link
        {
            public Transform transform;
            public CardinalAxisType axisType;
            public Link(Transform linkTransform, CardinalAxisType linkAxis)
            {
                transform = linkTransform;
                axisType = linkAxis;
            }

        }

        private List<Link> links = new List<Link>();
        private List<Renderer> linkRenderers = new List<Renderer>();

        internal void Clear()
        {
            if (links != null)
            {
                foreach (Link link in links)
                {
                    Object.Destroy(link.transform.gameObject);
                }
                links.Clear();
            }
        }

        private void SetMaterials()
        {
            //ensure materials
            if (wireframeMaterial == null)
            {
                wireframeMaterial = BoundsControlVisualUtils.CreateDefaultMaterial();
            }
        }


        internal void UpdateVisibilityInInspector(HideFlags flags)
        {
            if (links != null)
            {
                foreach (var link in links)
                {
                    link.transform.hideFlags = flags;
                }
            }
        }

        internal void ResetVisibility(bool isVisible)
        {
            if (links != null)
            {
                for (int i = 0; i < linkRenderers.Count; ++i)
                {
                    if (linkRenderers[i] != null)
                    {
                        linkRenderers[i].enabled = isVisible;
                    }
                }
            }
        }

        private Vector3 GetLinkDimensions(Vector3 currentBoundsExtents)
        {
            float linkLengthAdjustor = wireframeShape == WireframeType.Cubic ? 2.0f : 1.0f - (6.0f * wireframeEdgeRadius);
            return (currentBoundsExtents * linkLengthAdjustor) + new Vector3(wireframeEdgeRadius, wireframeEdgeRadius, wireframeEdgeRadius);
        }

        internal void UpdateLinkPositions(ref Vector3[] boundsCorners)
        {
            if (boundsCorners != null && links != null && links.Count == 12)
            {
                for (int i = 0; i < links.Count; ++i)
                {
                    links[i].transform.position = BoundsControlVisualUtils.GetLinkPosition(i, ref boundsCorners);
                }
            }
        }

        internal void UpdateLinkScales(Vector3 currentBoundsExtents)
        {
            if (links != null)
            {
                for (int i = 0; i < links.Count; ++i)
                {
                    Transform parent = links[i].transform.parent;
                    Vector3 rootScale = parent.lossyScale;
                    Vector3 invRootScale = new Vector3(1.0f / rootScale[0], 1.0f / rootScale[1], 1.0f / rootScale[2]);
                    // Compute the local scale that produces the desired world space dimensions
                    Vector3 linkDimensions = Vector3.Scale(GetLinkDimensions(currentBoundsExtents), invRootScale);

                    if (links[i].axisType == CardinalAxisType.X)
                    {
                        links[i].transform.localScale = new Vector3(wireframeEdgeRadius, linkDimensions.x, wireframeEdgeRadius);
                    }
                    else if (links[i].axisType == CardinalAxisType.Y)
                    {
                        links[i].transform.localScale = new Vector3(wireframeEdgeRadius, linkDimensions.y, wireframeEdgeRadius);
                    }
                    else//Z
                    {
                        links[i].transform.localScale = new Vector3(wireframeEdgeRadius, linkDimensions.z, wireframeEdgeRadius);
                    }
                }
            }
        }

        internal void Flatten(ref int[] flattenedHandles)
        {
            if (flattenedHandles != null && linkRenderers != null)
            {
                for (int i = 0; i < flattenedHandles.Length; ++i)
                {
                    linkRenderers[flattenedHandles[i]].enabled = false;
                }
            }
        }

        internal void CreateLinks(BoundsControlRotationHandles rotationHandles, Transform parent, Vector3 currentBoundsExtents)
        {
            // ensure materials exist 
            SetMaterials();

            // create links
            if (links != null)
            {
                GameObject link;
                Vector3 linkDimensions = GetLinkDimensions(currentBoundsExtents);
                for (int i = 0; i < BoundsControlRotationHandles.NumEdges; ++i)
                {
                    if (wireframeShape == WireframeType.Cubic)
                    {
                        link = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        GameObject.Destroy(link.GetComponent<BoxCollider>());
                    }
                    else
                    {
                        link = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        GameObject.Destroy(link.GetComponent<CapsuleCollider>());
                    }
                    link.name = "link_" + i.ToString();

                    CardinalAxisType axisType = rotationHandles.GetAxisType(i);
                    if (axisType == CardinalAxisType.Y)
                    {
                        link.transform.localScale = new Vector3(wireframeEdgeRadius, linkDimensions.y, wireframeEdgeRadius);
                        link.transform.Rotate(new Vector3(0.0f, 90.0f, 0.0f));
                    }
                    else if (axisType == CardinalAxisType.Z)
                    {
                        link.transform.localScale = new Vector3(wireframeEdgeRadius, linkDimensions.z, wireframeEdgeRadius);
                        link.transform.Rotate(new Vector3(90.0f, 0.0f, 0.0f));
                    }
                    else//X
                    {
                        link.transform.localScale = new Vector3(wireframeEdgeRadius, linkDimensions.x, wireframeEdgeRadius);
                        link.transform.Rotate(new Vector3(0.0f, 0.0f, 90.0f));
                    }

                    link.transform.position = rotationHandles.GetEdgeCenter(i);
                    link.transform.parent = parent;
                    Renderer linkRenderer = link.GetComponent<Renderer>();
                    linkRenderers.Add(linkRenderer);

                    if (wireframeMaterial != null)
                    {
                        linkRenderer.material = wireframeMaterial;
                    }

                    links.Add(new Link(link.transform, axisType));
                }
            }
        }
    }
}
