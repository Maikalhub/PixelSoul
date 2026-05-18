using UnityEngine;
using System.Collections.Generic;

namespace Lightbug.LaserMachine
{
    public class LaserMachine : MonoBehaviour
    {
        private struct LaserElement
        {
            public Transform transform;
            public LineRenderer lineRenderer;
            public GameObject sparks;
            public bool impact;
        }

        private readonly List<LaserElement> elementsList = new List<LaserElement>();

        [Header("External Data")]
        [SerializeField] private LaserData m_data;

        [Tooltip("This variable is true by default, all the inspector properties will be overridden.")]
        [SerializeField] private bool m_overrideExternalProperties = true;

        [SerializeField] private LaserProperties m_inspectorProperties = new LaserProperties();

        [Header("Render Order")]
        [SerializeField] private string m_sortingLayerName = "Default";
        [SerializeField] private int m_sortingOrder = 500;

        [Header("Render Queue")]
        [SerializeField] private bool m_forceMaterialRenderQueue = true;
        [SerializeField] private int m_materialRenderQueue = 4000;

        [Header("Visual Offset")]
        [SerializeField] private bool m_useVisualPositionOffset = true;
        [SerializeField] private Vector3 m_visualPositionOffset = new Vector3(0f, 0f, -0.1f);

        [Header("Player Damage")]
        [SerializeField] private bool damagePlayer = true;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private int damageAmount = 1;
        [SerializeField] private float damageCooldown = 0.5f;

        [Tooltip("Если true, лазер напрямую уменьшает currentHealth у PlayerMovement.")]
        [SerializeField] private bool damagePlayerMovementDirectly = true;

        [Tooltip("Если true, лазер также вызовет метод TakeDamage(int damage), если он есть на игроке.")]
        [SerializeField] private bool callTakeDamageMethod = false;

        [SerializeField] private string takeDamageMethodName = "TakeDamage";

        private readonly Dictionary<GameObject, float> lastDamageTimes = new Dictionary<GameObject, float>();

        private LaserProperties m_currentProperties;

        private float m_time = 0f;
        private bool m_active = true;
        private bool m_assignLaserMaterial;
        private bool m_assignSparks;

        private void OnEnable()
        {
            ClearLaserElements();

            m_currentProperties = m_overrideExternalProperties || m_data == null
                ? m_inspectorProperties
                : m_data.m_properties;

            m_currentProperties.m_initialTimingPhase =
                Mathf.Clamp01(m_currentProperties.m_initialTimingPhase);

            m_time =
                m_currentProperties.m_initialTimingPhase *
                m_currentProperties.m_intervalTime;

            float angleStep =
                m_currentProperties.m_raysNumber > 0
                    ? m_currentProperties.m_angularRange / m_currentProperties.m_raysNumber
                    : 0f;

            m_assignSparks =
                m_data != null &&
                m_data.m_laserSparks != null;

            m_assignLaserMaterial =
                m_data != null &&
                m_data.m_laserMaterial != null;

            for (int i = 0; i < m_currentProperties.m_raysNumber; i++)
            {
                LaserElement element = new LaserElement();

                GameObject newObj = new GameObject("lineRenderer_" + i);

                if (m_currentProperties.m_physicsType == LaserProperties.PhysicsType.Physics2D)
                    newObj.transform.position = (Vector2)transform.position;
                else
                    newObj.transform.position = transform.position;

                newObj.transform.rotation = transform.rotation;
                newObj.transform.Rotate(Vector3.up, i * angleStep);
                newObj.transform.position +=
                    newObj.transform.forward * m_currentProperties.m_minRadialDistance;

                LineRenderer lineRenderer = newObj.AddComponent<LineRenderer>();

                if (m_assignLaserMaterial)
                    lineRenderer.material = m_data.m_laserMaterial;

                SetupLineRenderer(lineRenderer);

                Vector3 startPoint = newObj.transform.position;
                Vector3 endPoint =
                    newObj.transform.position +
                    transform.forward * m_currentProperties.m_maxRadialDistance;

                lineRenderer.SetPosition(0, GetVisualPoint(startPoint));
                lineRenderer.SetPosition(1, GetVisualPoint(endPoint));

                newObj.transform.SetParent(transform);

                if (m_assignSparks)
                {
                    GameObject sparks = Instantiate(m_data.m_laserSparks);
                    sparks.transform.SetParent(newObj.transform);
                    sparks.SetActive(false);

                    SetupSparksRenderer(sparks);

                    element.sparks = sparks;
                }

                element.transform = newObj.transform;
                element.lineRenderer = lineRenderer;
                element.impact = false;

                elementsList.Add(element);
            }
        }

        private void OnDisable()
        {
            ClearLaserElements();
        }

        private void Update()
        {
            if (m_currentProperties == null)
                return;

            if (m_currentProperties.m_intermittent)
            {
                m_time += Time.deltaTime;

                if (m_time >= m_currentProperties.m_intervalTime)
                {
                    m_active = !m_active;
                    m_time = 0f;
                    return;
                }
            }

            RaycastHit2D hitInfo2D;
            RaycastHit hitInfo3D;

            foreach (LaserElement element in elementsList)
            {
                if (element.lineRenderer == null)
                    continue;

                if (m_currentProperties.m_rotate)
                {
                    if (m_currentProperties.m_rotateClockwise)
                    {
                        element.transform.RotateAround(
                            transform.position,
                            transform.up,
                            Time.deltaTime * m_currentProperties.m_rotationSpeed
                        );
                    }
                    else
                    {
                        element.transform.RotateAround(
                            transform.position,
                            transform.up,
                            -Time.deltaTime * m_currentProperties.m_rotationSpeed
                        );
                    }
                }

                if (m_active)
                {
                    element.lineRenderer.enabled = true;

                    Vector3 startPoint = element.transform.position;
                    element.lineRenderer.SetPosition(0, GetVisualPoint(startPoint));

                    if (m_currentProperties.m_physicsType == LaserProperties.PhysicsType.Physics3D)
                    {
                        Vector3 castEndPoint =
                            element.transform.position +
                            element.transform.forward * m_currentProperties.m_maxRadialDistance;

                        Physics.Linecast(
                            element.transform.position,
                            castEndPoint,
                            out hitInfo3D,
                            m_currentProperties.m_layerMask
                        );

                        if (hitInfo3D.collider)
                        {
                            element.lineRenderer.SetPosition(
                                1,
                                GetVisualPoint(hitInfo3D.point)
                            );

                            TryDamagePlayer(hitInfo3D.collider);

                            if (m_assignSparks && element.sparks != null)
                            {
                                element.sparks.transform.position =
                                    GetVisualPoint(hitInfo3D.point);

                                element.sparks.transform.rotation =
                                    Quaternion.LookRotation(hitInfo3D.normal);
                            }
                        }
                        else
                        {
                            element.lineRenderer.SetPosition(
                                1,
                                GetVisualPoint(castEndPoint)
                            );
                        }

                        if (m_assignSparks && element.sparks != null)
                            element.sparks.SetActive(hitInfo3D.collider != null);
                    }
                    else
                    {
                        Vector3 castEndPoint =
                            element.transform.position +
                            element.transform.forward * m_currentProperties.m_maxRadialDistance;

                        hitInfo2D = Physics2D.Linecast(
                            element.transform.position,
                            castEndPoint,
                            m_currentProperties.m_layerMask
                        );

                        if (hitInfo2D.collider)
                        {
                            Vector3 hitPoint = new Vector3(
                                hitInfo2D.point.x,
                                hitInfo2D.point.y,
                                element.transform.position.z
                            );

                            element.lineRenderer.SetPosition(
                                1,
                                GetVisualPoint(hitPoint)
                            );

                            TryDamagePlayer(hitInfo2D.collider);

                            if (m_assignSparks && element.sparks != null)
                            {
                                element.sparks.transform.position =
                                    GetVisualPoint(hitPoint);

                                element.sparks.transform.rotation =
                                    Quaternion.LookRotation(hitInfo2D.normal);
                            }
                        }
                        else
                        {
                            element.lineRenderer.SetPosition(
                                1,
                                GetVisualPoint(castEndPoint)
                            );
                        }

                        if (m_assignSparks && element.sparks != null)
                            element.sparks.SetActive(hitInfo2D.collider != null);
                    }
                }
                else
                {
                    element.lineRenderer.enabled = false;

                    if (m_assignSparks && element.sparks != null)
                        element.sparks.SetActive(false);
                }
            }
        }

        private void TryDamagePlayer(Collider2D hitCollider)
        {
            if (!damagePlayer)
                return;

            if (hitCollider == null)
                return;

            GameObject playerObject = FindTaggedObjectInParents(hitCollider.transform, playerTag);

            if (playerObject == null)
                return;

            TryApplyDamage(playerObject);
        }

        private void TryDamagePlayer(Collider hitCollider)
        {
            if (!damagePlayer)
                return;

            if (hitCollider == null)
                return;

            GameObject playerObject = FindTaggedObjectInParents(hitCollider.transform, playerTag);

            if (playerObject == null)
                return;

            TryApplyDamage(playerObject);
        }

        private GameObject FindTaggedObjectInParents(Transform startTransform, string targetTag)
        {
            if (startTransform == null)
                return null;

            Transform current = startTransform;

            while (current != null)
            {
                if (current.CompareTag(targetTag))
                    return current.gameObject;

                current = current.parent;
            }

            return null;
        }

        private void TryApplyDamage(GameObject playerObject)
        {
            if (playerObject == null)
                return;

            if (damageAmount <= 0)
                return;

            if (damageCooldown > 0f)
            {
                if (lastDamageTimes.TryGetValue(playerObject, out float lastTime))
                {
                    if (Time.time < lastTime + damageCooldown)
                        return;
                }

                lastDamageTimes[playerObject] = Time.time;
            }

            if (damagePlayerMovementDirectly)
                DamagePlayerMovement(playerObject);

            if (callTakeDamageMethod)
            {
                playerObject.SendMessage(
                    takeDamageMethodName,
                    damageAmount,
                    SendMessageOptions.DontRequireReceiver
                );
            }
        }

        private void DamagePlayerMovement(GameObject playerObject)
        {
            PlayerMovement player = playerObject.GetComponent<PlayerMovement>();

            if (player == null)
                player = playerObject.GetComponentInChildren<PlayerMovement>();

            if (player == null)
                player = playerObject.GetComponentInParent<PlayerMovement>();

            if (player == null)
                return;

            if (player.isDead)
                return;

            int finalDamage = damageAmount;

            if (player.defenseStat > 0)
                finalDamage = Mathf.Max(1, damageAmount - player.defenseStat);

            player.currentHealth -= finalDamage;
            player.currentHealth = Mathf.Max(0, player.currentHealth);

            Debug.Log($"Laser damaged player: -{finalDamage} HP. Current HP: {player.currentHealth}");
        }

        private void SetupLineRenderer(LineRenderer lineRenderer)
        {
            if (lineRenderer == null)
                return;

            lineRenderer.receiveShadows = false;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            lineRenderer.startWidth = m_currentProperties.m_rayWidth;
            lineRenderer.endWidth = m_currentProperties.m_rayWidth;

            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 2;

            lineRenderer.sortingLayerName = m_sortingLayerName;
            lineRenderer.sortingOrder = m_sortingOrder;

            lineRenderer.numCapVertices = 4;
            lineRenderer.numCornerVertices = 4;

            if (m_forceMaterialRenderQueue && lineRenderer.material != null)
                lineRenderer.material.renderQueue = m_materialRenderQueue;
        }

        private void SetupSparksRenderer(GameObject sparks)
        {
            if (sparks == null)
                return;

            Renderer[] renderers = sparks.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                renderer.sortingLayerName = m_sortingLayerName;
                renderer.sortingOrder = m_sortingOrder + 1;

                if (m_forceMaterialRenderQueue && renderer.material != null)
                    renderer.material.renderQueue = m_materialRenderQueue + 1;
            }
        }

        private Vector3 GetVisualPoint(Vector3 point)
        {
            if (!m_useVisualPositionOffset)
                return point;

            return point + m_visualPositionOffset;
        }

        private void ClearLaserElements()
        {
            for (int i = elementsList.Count - 1; i >= 0; i--)
            {
                if (elementsList[i].transform != null)
                    Destroy(elementsList[i].transform.gameObject);
            }

            elementsList.Clear();
        }
    }
}