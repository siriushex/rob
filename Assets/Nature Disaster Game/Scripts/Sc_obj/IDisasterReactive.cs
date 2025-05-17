using UnityEngine;
public interface IDisasterReactive
{
    void OnFlood();        // ������� �� ����������
    void OnTornado(); // ����������
    void OnAcidRain(Material[] materialsList);     // ����������� � �.�.
    void OnVolcanoEroption();     
    void OnBlackHole();     
}
