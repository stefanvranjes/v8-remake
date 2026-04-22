using UnityEngine;

namespace V8Remake.Core
{
    public class GameManager : MonoBehaviour
    {
        [Header("Prototype Settings")]
        public Transform playerSpawnPoint;
        public GameObject playerVehiclePrefab;
        
        [Header("UI (To Be Implemented)")]
        public GameObject gameOverScreen;
        public GameObject winScreen;

        private GameObject currentPlayerVehicle;

        void Start()
        {
            StartMatch();
        }

        public void StartMatch()
        {
            if (playerVehiclePrefab != null && playerSpawnPoint != null)
            {
                currentPlayerVehicle = Instantiate(playerVehiclePrefab, playerSpawnPoint.position, playerSpawnPoint.rotation);
                
                // Assign camera target here if using Cinemachine or custom camera script
                Vehicle.VehicleCamera cam = Camera.main.gameObject.GetComponent<Vehicle.VehicleCamera>();
                if (cam != null)
                {
                    cam.target = currentPlayerVehicle.transform;
                }
            }
            else
            {
                Debug.LogWarning("Player vehicle prefab or spawn point not assigned in GameManager.");
            }
        }

        public void OnVehicleDestroyed(GameObject vehicle)
        {
            if (vehicle == currentPlayerVehicle)
            {
                GameOver();
            }
            else
            {
                // Handle enemy destruction (score, check win condition)
            }
        }

        private void GameOver()
        {
            Debug.Log("Game Over!");
            if (gameOverScreen != null)
            {
                gameOverScreen.SetActive(true);
            }
        }
    }
}
