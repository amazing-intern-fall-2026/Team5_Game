using UnityEngine;
using UnityEngine.Tilemaps;

public class SeasonGroundController : MonoBehaviour
{
    [Header("Season Ground Tilemaps")]
    [SerializeField] private Tilemap springGround;
    [SerializeField] private Tilemap summerGround;
    [SerializeField] private Tilemap autumnGround;
    [SerializeField] private Tilemap winterGround;

private void Start()
    {
        if (SeasonManager.Instance == null)
        {
            Debug.LogError(
                "SeasonGroundController: " +
                "Không tìm thấy SeasonManager!"
            );

            return;
        }

        // Đăng ký sự kiện đổi mùa
        SeasonManager.Instance.OnSeasonChanged += ChangeSeason;

        // Hiển thị đúng mùa ngay khi bắt đầu game
        ChangeSeason(
            SeasonManager.Instance.CurrentSeason
        );
    }


    private void OnDestroy()
    {
        if (SeasonManager.Instance != null)
        {
            SeasonManager.Instance.OnSeasonChanged -= ChangeSeason;
        }
    }


    private void ChangeSeason(
        SeasonType season
    )
    {
        // -------------------------------------------------
        // Tắt tất cả Tilemap trước
        // -------------------------------------------------

        SetTilemapActive(
            springGround,
            false
        );

        SetTilemapActive(
            summerGround,
            false
        );

        SetTilemapActive(
            autumnGround,
            false
        );

        SetTilemapActive(
            winterGround,
            false
        );


        // -------------------------------------------------
        // Bật Tilemap theo mùa
        // -------------------------------------------------

        switch (season)
        {
            case SeasonType.Spring:

                SetTilemapActive(
                    springGround,
                    true
                );

                Debug.Log(
                    "🌱 Ground: Spring"
                );

                break;


            case SeasonType.Summer:

                SetTilemapActive(
                    summerGround,
                    true
                );

                Debug.Log(
                    "☀️ Ground: Summer"
                );

                break;


            case SeasonType.Autumn:

                SetTilemapActive(
                    autumnGround,
                    true
                );

                Debug.Log(
                    "🍂 Ground: Autumn"
                );

                break;


            case SeasonType.Winter:

                SetTilemapActive(
                    winterGround,
                    true
                );

                Debug.Log(
                    "❄️ Ground: Winter"
                );

                break;
        }
    }


    private void SetTilemapActive(
        Tilemap tilemap,
        bool active
    )
    {
        if (tilemap != null)
        {
            tilemap.gameObject.SetActive(
                active
            );
        }
    }

}
