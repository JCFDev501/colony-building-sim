using UnityEngine;

public class BlockInstanceTester : MonoBehaviour
{
    [SerializeField] private BlockInstance m_blockInstance;
    [SerializeField] private BlockDefinition m_blockDefinition = null;
    [SerializeField] private Vector2Int m_tileCoordinates = Vector2Int.zero;

    private void Start()
    {
        if (m_blockInstance == null)
        {
            Debug.LogError("BlockInstanceTester is missing a BlockInstance reference.", this);
            return;
        }

        if (m_blockDefinition == null)
        {
            Debug.LogError("BlockInstanceTester is missing a BlockDefinition reference.", this);
            return;
        }

        m_blockInstance.Initialize(m_blockDefinition, m_tileCoordinates);
    }
}