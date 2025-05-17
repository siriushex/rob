using Invector.vCharacterController;
using UnityEngine;

public class TsunamiDisaster : MonoBehaviour
{
    [Header("��������� ������")]
    [SerializeField] private float minHeight = 1f;
    [SerializeField] private float maxHeight = 5f;

    [Header("��������")]
    public float speed = 4f;   // �/�
    public float endZ = 50f;   // �������� ������� �� Z

    [SerializeField] private float startDelay = 1f;

    private float startX;      // ��������� X
    private float startY;      // ��������� Y

    private void Start()
    {
        // 1. ��������� X, ���������� Y � ������ ������
        startX = transform.position.x;
        startY = Random.Range(minHeight, maxHeight);
        transform.position = new Vector3(startX, startY, transform.position.z);

        // 2. ��������� �����������
        StartCoroutine(MoveAlongZ());
    }

    private System.Collections.IEnumerator MoveAlongZ()
    {
        yield return new WaitForSeconds(startDelay);

        // ���� �� �������� endZ
        while (Mathf.Abs(transform.position.z - endZ) > 0.01f)
        {
            float newZ = Mathf.MoveTowards(
                transform.position.z,
                endZ,
                speed * Time.deltaTime);

            transform.position = new Vector3(startX, startY, newZ);
            yield return null;
        }

        Destroy(gameObject);   // ����� ��������������
    }

    void OnTriggerEnter(Collider collision)
    {
        if(collision.CompareTag("Player"))
        {
            Debug.Log("Tsunami hit player!");
           if( collision.gameObject.GetComponent<Invector.vHealthController>() != null)
           {
              Debug.Log("Tsunami hit player with Invector!");
             Invector.vDamage damage = new Invector.vDamage();
              damage.damageValue = 100f;
              damage.sender = collision.transform;   
              collision.gameObject.GetComponent<Invector.vHealthController>().TakeDamage(damage);
              Debug.Log("Tsunami hit player!");
           }
           else if (collision.gameObject.GetComponent<BotAI>() != null)
           {
              // collision.gameObject.GetComponent<BotAI>().TakeDamage(100f);
               Debug.Log("Tsunami hit bot!");
           }
        }
    }
}
