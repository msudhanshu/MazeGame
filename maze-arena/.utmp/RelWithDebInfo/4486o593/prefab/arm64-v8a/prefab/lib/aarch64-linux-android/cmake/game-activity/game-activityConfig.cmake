if(NOT TARGET game-activity::game-activity)
add_library(game-activity::game-activity STATIC IMPORTED)
set_target_properties(game-activity::game-activity PROPERTIES
    IMPORTED_LOCATION "/private/var/folders/g2/5y9nzl4x06b9pgm6fth9h5780000gn/T/cursor-sandbox-cache/98d6d722d72dfaccb5a49855960a784f/gradle/caches/9.1.0/transforms/1729aecbaee4d53c43e888c08a695309/transformed/jetified-games-activity-4.4.0/prefab/modules/game-activity/libs/android.arm64-v8a/libgame-activity.a"
    INTERFACE_INCLUDE_DIRECTORIES "/private/var/folders/g2/5y9nzl4x06b9pgm6fth9h5780000gn/T/cursor-sandbox-cache/98d6d722d72dfaccb5a49855960a784f/gradle/caches/9.1.0/transforms/1729aecbaee4d53c43e888c08a695309/transformed/jetified-games-activity-4.4.0/prefab/modules/game-activity/include"
    INTERFACE_LINK_LIBRARIES ""
)
endif()

if(NOT TARGET game-activity::game-activity_static)
add_library(game-activity::game-activity_static STATIC IMPORTED)
set_target_properties(game-activity::game-activity_static PROPERTIES
    IMPORTED_LOCATION "/private/var/folders/g2/5y9nzl4x06b9pgm6fth9h5780000gn/T/cursor-sandbox-cache/98d6d722d72dfaccb5a49855960a784f/gradle/caches/9.1.0/transforms/1729aecbaee4d53c43e888c08a695309/transformed/jetified-games-activity-4.4.0/prefab/modules/game-activity_static/libs/android.arm64-v8a/libgame-activity_static.a"
    INTERFACE_INCLUDE_DIRECTORIES "/private/var/folders/g2/5y9nzl4x06b9pgm6fth9h5780000gn/T/cursor-sandbox-cache/98d6d722d72dfaccb5a49855960a784f/gradle/caches/9.1.0/transforms/1729aecbaee4d53c43e888c08a695309/transformed/jetified-games-activity-4.4.0/prefab/modules/game-activity_static/include"
    INTERFACE_LINK_LIBRARIES ""
)
endif()

