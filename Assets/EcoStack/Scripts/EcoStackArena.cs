using UnityEngine;
namespace EcoStack
{
    public partial class EcoStackGame
    {
        void AddCombatLayout()
        {
            // Cover is persistent scene geometry and blocks both movement and projectiles.
            for(int z=34;z<145;z+=36)
                foreach(int side in new int[]{-1,1})
                {
                    Vector3 p=new Vector3(side*4.5f,.75f,z);
                    Block("Placa cobertura",p,new Vector3(2.6f,1.5f,1.3f),steel,true);
                    Block("Franja de seguridad",p+new Vector3(0,.77f,0),new Vector3(2.65f,.08f,1.35f),brass);
                    for(int i=0;i<3;i++) Block("Luz cobertura",p+new Vector3(-.8f+i*.8f,.3f,-.67f),new Vector3(.13f,.2f,.04f),Color.cyan);
                }
            for(int i=0;i<24;i++)
            {
                float angle=i*Mathf.PI*2/24;
                Transform mark=Block("Luz perímetro del núcleo",new Vector3(Mathf.Cos(angle)*10,.09f,164+Mathf.Sin(angle)*10),new Vector3(1.8f,.06f,.12f),new Color(1,.25f,.05f));
                mark.localRotation=Quaternion.Euler(0,-angle*Mathf.Rad2Deg,0);
            }
            Label("NÚCLEO DE SEGURIDAD",new Vector3(0,6.5f,158),environmentRoot,.1f);
            Label("ZONA SEGURA  /  RECUPERACIÓN",new Vector3(0,4.5f,186),environmentRoot,.08f);
            for(int z=20;z<160;z+=28)
            {
                Transform arrow=Block("Luz ruta de rescate",new Vector3(-11.5f,.08f,z),new Vector3(.16f,.04f,1.4f),Color.cyan);
                arrow.localRotation=Quaternion.Euler(0,25,0);
                Transform arrow2=Block("Luz ruta de rescate",new Vector3(-10.9f,.08f,z),new Vector3(.16f,.04f,1.4f),Color.cyan);
                arrow2.localRotation=Quaternion.Euler(0,-25,0);
            }
        }
    }
}
