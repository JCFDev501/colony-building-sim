using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColonyBuildingSim.Inventory
{
    /// <summary>
    /// Stores colony-level resource counts.
    /// </summary>
    public class ColonyInventoryManager : MonoBehaviour
    {
        [Serializable]
        private class ResourceAmount
        {
            [SerializeField] private ResourceType m_resourceType = ResourceType.Wood;
            [SerializeField] private int m_amount = 0;

            public ResourceType ResourceType
            {
                get { return m_resourceType; }
            }

            public int Amount
            {
                get { return m_amount; }
                set { m_amount = Mathf.Max(0, value); }
            }

            public ResourceAmount(ResourceType resourceType, int amount)
            {
                m_resourceType = resourceType;
                m_amount = Mathf.Max(0, amount);
            }
        }

        [Header("Runtime Inventory")]
        [SerializeField] private List<ResourceAmount> m_resourceAmounts = new List<ResourceAmount>();

        private readonly Dictionary<ResourceType, ResourceAmount> m_resourceLookup = new Dictionary<ResourceType, ResourceAmount>();

        /// <summary>
        /// Initializes the resource lookup when play begins.
        /// </summary>
        private void Awake()
        {
            RebuildResourceLookup();
            EnsureAllResourceTypesExist();
        }

        /// <summary>
        /// Adds the requested resource amount to the colony inventory.
        /// Non-positive amounts are ignored.
        /// </summary>
        public void AddResource(ResourceType resourceType, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            ResourceAmount resourceAmount = GetOrCreateResourceAmount(resourceType);
            resourceAmount.Amount += amount;
        }

        /// <summary>
        /// Attempts to remove the requested resource amount from the colony inventory.
        /// Returns false if the amount is invalid or insufficient.
        /// </summary>
        public bool TryRemoveResource(ResourceType resourceType, int amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            ResourceAmount resourceAmount = GetOrCreateResourceAmount(resourceType);

            if (resourceAmount.Amount < amount)
            {
                return false;
            }

            resourceAmount.Amount -= amount;
            return true;
        }

        /// <summary>
        /// Returns the current stored amount for the requested resource.
        /// Missing resources return 0.
        /// </summary>
        public int GetResourceAmount(ResourceType resourceType)
        {
            if (!m_resourceLookup.TryGetValue(resourceType, out ResourceAmount resourceAmount))
            {
                return 0;
            }

            return resourceAmount.Amount;
        }

        /// <summary>
        /// Returns true when the colony has at least the requested amount of a resource.
        /// </summary>
        public bool HasResource(ResourceType resourceType, int amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            return GetResourceAmount(resourceType) >= amount;
        }

        /// <summary>
        /// Returns a snapshot of all current resource amounts.
        /// </summary>
        public Dictionary<ResourceType, int> GetAllResourceAmounts()
        {
            Dictionary<ResourceType, int> resourceAmounts = new Dictionary<ResourceType, int>();

            foreach (ResourceType resourceType in Enum.GetValues(typeof(ResourceType)))
            {
                resourceAmounts[resourceType] = GetResourceAmount(resourceType);
            }

            return resourceAmounts;
        }

        /// <summary>
        /// Rebuilds the lookup from the serialized list so Inspector-visible data stays queryable.
        /// </summary>
        private void RebuildResourceLookup()
        {
            m_resourceLookup.Clear();

            for (int i = 0; i < m_resourceAmounts.Count; i++)
            {
                ResourceAmount resourceAmount = m_resourceAmounts[i];

                if (resourceAmount == null)
                {
                    continue;
                }

                if (m_resourceLookup.ContainsKey(resourceAmount.ResourceType))
                {
                    Debug.LogWarning("Duplicate resource amount found for " + resourceAmount.ResourceType + ".", this);
                    continue;
                }

                m_resourceLookup.Add(resourceAmount.ResourceType, resourceAmount);
            }
        }

        /// <summary>
        /// Ensures every ResourceType has an entry so debug panels can show zero-count resources.
        /// </summary>
        private void EnsureAllResourceTypesExist()
        {
            foreach (ResourceType resourceType in Enum.GetValues(typeof(ResourceType)))
            {
                GetOrCreateResourceAmount(resourceType);
            }
        }

        /// <summary>
        /// Gets an existing resource amount entry or creates one with amount 0.
        /// </summary>
        private ResourceAmount GetOrCreateResourceAmount(ResourceType resourceType)
        {
            if (m_resourceLookup.TryGetValue(resourceType, out ResourceAmount resourceAmount))
            {
                return resourceAmount;
            }

            ResourceAmount newResourceAmount = new ResourceAmount(resourceType, 0);
            m_resourceAmounts.Add(newResourceAmount);
            m_resourceLookup.Add(resourceType, newResourceAmount);

            return newResourceAmount;
        }
    }
}