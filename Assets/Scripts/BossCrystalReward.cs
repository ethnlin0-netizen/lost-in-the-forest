using UnityEngine;

public class BossCrystalReward : MonoBehaviour
{
    public EnemyHealth boss;
    public GameObject crystal;
    public bool Revealed { get; private set; }

    private void OnEnable()
    {
        if(boss==null||crystal==null)return;
        boss.Died+=Reveal;
        if(boss.IsDead)Reveal();
        else if(!Revealed)crystal.SetActive(false);
    }
    private void OnDisable()
    {
        if(boss!=null)boss.Died-=Reveal;
    }
    private void Reveal()
    {
        if(Revealed||crystal==null)return;
        Revealed=true;
        crystal.SetActive(true);
        crystal.GetComponent<GameplaySfx>()?.Play(GameSound.CrystalAppear);
    }
}
