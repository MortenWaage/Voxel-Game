using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{

    public int seed;
    public BiomeAttribute biome;

    public Transform player;
    public Player playerObject;

    public Vector3 spawnPosition;

    public Material material;
    public BlockType[] blocktypes;

    Chunk[,] chunks = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];

    List<ChunkCoord> activeChunks = new List<ChunkCoord>();
    public ChunkCoord playerChunkCoord;
    ChunkCoord playerLastChunkCoord;

    List<ChunkCoord> chunksToCreate = new List<ChunkCoord>();
    private bool isCreatingChunks;

    public GameObject debugScreen;

    public int terrainCutoff = 25; //Setter en minimumsverdi for terreghøyde (En høyde fra Perlin noise på f.ex 20 vil settes til 25 for å gi mer naturlig terreng.

    private void Start()
    {
        

        Random.InitState(seed);

        int _xZ = ((VoxelData.WorldSizeInChunks / 2) - VoxelData.ViewDistanceInChunks) * VoxelData.ChunkWidth;
        int _xZOffSet = (VoxelData.ViewDistanceInChunks * VoxelData.ChunkWidth);
        int _y = VoxelData.ChunkHeight;


        spawnPosition = new Vector3(_xZ + _xZOffSet, _y, _xZ + _xZOffSet);

        GenerateWorld();

        player.position = CheckSpawnPos(spawnPosition);

        playerLastChunkCoord = GetChunkCoordFromVector3(player.position);


    }

    private void Update()
    {

        playerChunkCoord = GetChunkCoordFromVector3(player.position);

        // Oppdater chunks om spilleren har flyttet seg siden sist frame
        if (!playerChunkCoord.Equals(playerLastChunkCoord))
            CheckViewDistance();

        if (chunksToCreate.Count > 0 && !isCreatingChunks)
            StartCoroutine("CreateChunks");

        // Åpne debugscreen
        if (Input.GetKeyDown(KeyCode.F3))
            debugScreen.SetActive(!debugScreen.activeSelf);


    }

    void GenerateWorld()
    {
        for (int x = (VoxelData.WorldSizeInChunks / 2) - VoxelData.ViewDistanceInChunks; x < (VoxelData.WorldSizeInChunks / 2) + VoxelData.ViewDistanceInChunks; x++)
        {
            for (int z = (VoxelData.WorldSizeInChunks / 2) - VoxelData.ViewDistanceInChunks; z < (VoxelData.WorldSizeInChunks / 2) + VoxelData.ViewDistanceInChunks; z++)
            {

                chunks[x, z] = new Chunk(new ChunkCoord(x, z), this, true);
                activeChunks.Add(new ChunkCoord(x, z));

            }
        }


        

    }

    private Vector3 CheckSpawnPos(Vector3 _spawnPosition)
    {

        Vector3 spawnPos = _spawnPosition;

        bool gotPos = false;

        int timeOut = VoxelData.ChunkHeight;

        while (gotPos == false)
        {
            while (!CheckForVoxel(new Vector3(spawnPos.x, spawnPos.y - 2f, spawnPos.z)))
            {
                spawnPos.y -= 1;
                timeOut--;

                if (spawnPos.y < defaultTerrainHeight)
                {
                    spawnPos.y = _spawnPosition.y;
                    spawnPos.x -= 1;
                    spawnPos.z += 1;
                    timeOut = VoxelData.ChunkHeight;
                }

                if (timeOut < 0)
                {
                    Debug.Log("Error in CheckSpawnPos() - Unable to find ground");
                    break;
                }
            }

            if (CheckForVoxel(new Vector3(spawnPos.x, spawnPos.y - 2f, spawnPos.z)))
            {
                gotPos = true;
            }
            else
                Debug.Log("Error in CheckSpawnPos() - Unable to find suitable spawn point");
                break;
        }


        return new Vector3(spawnPos.x, spawnPos.y, spawnPos.z);
    }

    IEnumerator CreateChunks()
    {

        isCreatingChunks = true;

        while (chunksToCreate.Count > 0)
        {

            chunks[chunksToCreate[0].x, chunksToCreate[0].z].Init();
            chunksToCreate.RemoveAt(0);
            yield return null;

        }

        isCreatingChunks = false;

    }

    ChunkCoord GetChunkCoordFromVector3(Vector3 pos)
    {

        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);
        return new ChunkCoord(x, z);

    }

    void CheckViewDistance()
    {

        ChunkCoord coord = GetChunkCoordFromVector3(player.position);
        playerLastChunkCoord = playerChunkCoord;

        List<ChunkCoord> previouslyActiveChunks = new List<ChunkCoord>(activeChunks);

        // Loop gjennom alle chunks i view distance
        for (int x = coord.x - VoxelData.ViewDistanceInChunks; x < coord.x + VoxelData.ViewDistanceInChunks; x++)
        {
            for (int z = coord.z - VoxelData.ViewDistanceInChunks; z < coord.z + VoxelData.ViewDistanceInChunks; z++)
            {

                // Hvis chunken finnes i verden...
                if (IsChunkInWorld(new ChunkCoord(x, z)))
                {

                    // Hvis chunken ikke er aktv, aktiver den
                    if (chunks[x, z] == null)
                    {
                        chunks[x, z] = new Chunk(new ChunkCoord(x, z), this, false);
                        chunksToCreate.Add(new ChunkCoord(x, z));
                    }
                    else if (!chunks[x, z].isActive)
                    {
                        chunks[x, z].isActive = true;
                    }
                    activeChunks.Add(new ChunkCoord(x, z));
                }

                // Sjekk tidligere aktive chunks for å se om den enda finnes. Hvis den gjør det, fjern fra lista
                for (int i = 0; i < previouslyActiveChunks.Count; i++)
                {

                    if (previouslyActiveChunks[i].Equals(new ChunkCoord(x, z)))
                        previouslyActiveChunks.RemoveAt(i);

                }

            }
        }

        // Skru av chunks utenfor view distance
        foreach (ChunkCoord c in previouslyActiveChunks)
            chunks[c.x, c.z].isActive = false;

    }

    public bool CheckForVoxel(Vector3 pos)
    {

        ChunkCoord thisChunk = new ChunkCoord(pos);

        if (!IsChunkInWorld(thisChunk) || pos.y < 0 || pos.y > VoxelData.ChunkHeight)
            return false;

        if (chunks[thisChunk.x, thisChunk.z] != null && chunks[thisChunk.x, thisChunk.z].isVoxelMapPopulated)
            return blocktypes[chunks[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos)].isSolid;

        return blocktypes[GetVoxel(pos)].isSolid;

    }


    public byte GetVoxel(Vector3 pos)
    {

        int yPos = Mathf.FloorToInt(pos.y);

        /* Luft og Bedrock */

        // Luft
        if (!IsVoxelInWorld(pos))
            return 0;

        // Bedrock
        if (yPos == 0)
            return 1;

        /* Første pass av terreng-generering */

        int terrainHeight = Mathf.FloorToInt(biome.terrainHeight * Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.terrainScale)) + biome.solidGroundHeight;              


        terrainHeight = Mathf.Max(defaultTerrainHeight, terrainHeight);
        byte voxelValue = 0;

        if (yPos == terrainHeight)
            voxelValue = 3;
        else if (yPos < terrainHeight && yPos > terrainHeight - 4)
            voxelValue = 4;
        else if (yPos > terrainHeight)
            return 0;
        else
            voxelValue = 2;
     
        /* Mineraler og andre blokker */

        if (voxelValue == 2)
        {

            foreach (Lode lode in biome.lodes)
            {

                if (yPos > lode.minHeight && yPos < lode.maxHeight)
                    if (Noise.Get3DPerlin(pos, lode.noiseOffset, lode.scale, lode.threshhold))
                        voxelValue = lode.blockID;

            }

        }


        //Veldig basic cave. Må se på Perlin Worms-algoritmer
        foreach (Lode lode in biome.lodes)
        {
            if (lode.nodeName == "Caves")
            {
                if (Noise.Get3DPerlin(pos, lode.noiseOffset, lode.scale, lode.threshhold))
                    voxelValue = lode.blockID;
            }            
        }



        return voxelValue;


    }

    bool IsChunkInWorld(ChunkCoord coord)
    {

        if (coord.x > 0 && coord.x < VoxelData.WorldSizeInChunks - 1 && coord.z > 0 && coord.z < VoxelData.WorldSizeInChunks - 1)
            return true;
        else
            return
                false;

    }

    bool IsVoxelInWorld(Vector3 pos)
    {

        if (pos.x >= 0 && pos.x < VoxelData.WorldSizeInVoxels && pos.y >= 0 && pos.y < VoxelData.ChunkHeight && pos.z >= 0 && pos.z < VoxelData.WorldSizeInVoxels)
            return true;
        else
            return false;

    }

}

[System.Serializable]
public class BlockType
{

    public string blockName;
    public bool isSolid;

    [Header("Texture Values")]
    public int backFaceTexture;
    public int frontFaceTexture;
    public int topFaceTexture;
    public int bottomFaceTexture;
    public int leftFaceTexture;
    public int rightFaceTexture;

    // Back, Front, Top, Bottom, Left, Right

    public int GetTextureID(int faceIndex)
    {

        switch (faceIndex)
        {

            case 0:
                return backFaceTexture;
            case 1:
                return frontFaceTexture;
            case 2:
                return topFaceTexture;
            case 3:
                return bottomFaceTexture;
            case 4:
                return leftFaceTexture;
            case 5:
                return rightFaceTexture;
            default:
                Debug.Log("Error in GetTextureID; invalid face index");
                return 0;


        }

    }

}
