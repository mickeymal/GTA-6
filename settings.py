"""Vice Bay Stories - global constants and configuration."""

# ---------------------------------------------------------------- screen
SCREEN_W = 1280
SCREEN_H = 720
FPS = 60
TITLE = "Vice Bay Stories"

# ---------------------------------------------------------------- world
TILE = 32                       # pixels per tile
MAP_TILES = 192                 # map is MAP_TILES x MAP_TILES
MAP_W = MAP_TILES * TILE
MAP_H = MAP_TILES * TILE
WORLD_SEED = 6                  # deterministic city layout

# tile types
T_WATER = 0
T_SAND = 1
T_GRASS = 2
T_ROAD = 3
T_SIDEWALK = 4
T_BUILDING = 5
T_RUNWAY = 6
T_DOCK = 7
T_PARK = 8

# which tiles block whom
FOOT_BLOCKED = {T_BUILDING}                 # player can swim in water (slow)
CAR_BLOCKED = {T_BUILDING, T_WATER}
BOAT_ALLOWED = {T_WATER}
SLOW_TILES = {T_SAND, T_GRASS, T_PARK}      # ground vehicles slow down here

# ---------------------------------------------------------------- time / weather
DAY_LENGTH_SECONDS = 480.0      # one full 24h cycle in real seconds
WEATHER_CLEAR = "clear"
WEATHER_RAIN = "rain"
WEATHER_STORM = "storm"
WEATHER_FOG = "fog"
WEATHER_KINDS = (WEATHER_CLEAR, WEATHER_CLEAR, WEATHER_RAIN, WEATHER_FOG, WEATHER_STORM)

# ---------------------------------------------------------------- colors
C_WATER = (18, 46, 84)
C_WATER_DEEP = (10, 30, 62)
C_SAND = (222, 200, 148)
C_GRASS = (58, 112, 62)
C_ROAD = (52, 52, 58)
C_ROAD_LINE = (188, 172, 60)
C_SIDEWALK = (128, 126, 122)
C_RUNWAY = (70, 70, 76)
C_DOCK = (110, 88, 62)
C_PARK = (46, 128, 70)
NEON_COLORS = [(255, 60, 180), (60, 220, 255), (170, 90, 255), (255, 160, 60), (80, 255, 160)]
DISTRICT_BUILDING = {
    "downtown": [(90, 96, 118), (72, 80, 104), (108, 112, 134)],
    "beach": [(214, 150, 170), (150, 196, 214), (222, 196, 150)],
    "haiti": [(150, 122, 96), (170, 150, 110), (128, 108, 88)],
    "suburbs": [(168, 152, 130), (188, 172, 146), (148, 140, 120)],
    "port": [(96, 92, 88), (116, 108, 96), (84, 84, 90)],
    "airport": [(120, 124, 130), (100, 104, 112)],
    "midtown": [(120, 118, 128), (104, 104, 116), (134, 130, 138)],
}

UI_BG = (12, 12, 20, 190)
UI_ACCENT = (255, 60, 180)
UI_ACCENT2 = (60, 220, 255)
UI_TEXT = (235, 235, 240)
UI_WARN = (255, 90, 70)
UI_MONEY = (110, 230, 120)

# ---------------------------------------------------------------- player
PLAYER_SPEED = 150.0
PLAYER_SPRINT = 245.0
PLAYER_SWIM = 70.0
PLAYER_MAX_HP = 100
PLAYER_MAX_ARMOR = 100
PLAYER_RADIUS = 10

# ---------------------------------------------------------------- police
WANTED_DECAY_TIME = 22.0        # seconds out of sight per star lost
POLICE_SIGHT = 420.0
MAX_STARS = 5

# ---------------------------------------------------------------- misc
MINIMAP_SIZE = 176
NOTIFY_TIME = 4.0
SAVE_DIR = "saves"
STAR_CHAR = "★"
