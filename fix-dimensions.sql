BEGIN;

-- A1) Legit completers (killed King Terror via Kingslayer=80): make sure the
--     unlock quest 900000 is marked completed.
UPDATE quest_status
SET serialized_data = json_patch(serialized_data, json_object(
    'Completed', CASE WHEN EXISTS (SELECT 1 FROM json_each(json_extract(serialized_data,'$.Completed')) WHERE value = 900000)
                      THEN json_extract(serialized_data,'$.Completed')
                      ELSE json_insert(json_extract(serialized_data,'$.Completed'), '$[#]', 900000) END,
    'Progress',  (SELECT json_group_array(value) FROM json_each(json_extract(serialized_data,'$.Progress')) WHERE json_extract(value,'$.id') <> 900000)))
WHERE EXISTS (SELECT 1 FROM json_each(json_extract(serialized_data,'$.Completed')) WHERE value = 80)
  AND (NOT EXISTS (SELECT 1 FROM json_each(json_extract(serialized_data,'$.Completed')) WHERE value = 900000)
       OR EXISTS (SELECT 1 FROM json_each(json_extract(serialized_data,'$.Progress')) WHERE json_extract(value,'$.id') = 900000));

-- A2) Grant them the unlock: raise dimension.max to 1 (never lowers it).
UPDATE players
SET player_properties = json_patch(
        CASE WHEN COALESCE(player_properties,'') = '' THEN '{}' ELSE player_properties END,
        json_object('dimension.max', 1))
WHERE player_id IN (
        SELECT player_id FROM quest_status
        WHERE EXISTS (SELECT 1 FROM json_each(json_extract(serialized_data,'$.Completed')) WHERE value = 80))
  AND COALESCE(CAST(json_extract(
        CASE WHEN COALESCE(player_properties,'') = '' THEN '{}' ELSE player_properties END,
        '$."dimension.max"') AS INTEGER), 0) < 1;

-- B) Free completions during the stripped window: no Kingslayer, no unlock
--     granted => the completion was the bug. Put the quest back to STARTED:
--     drop the completion, ensure it is in Started, and re-create the kill
--     progress entry (CompleteQuest deleted it; without it kills never credit,
--     because StartQuest early-returns for quests already in Started).
UPDATE quest_status
SET serialized_data = json_patch(serialized_data, json_object(
    'Completed', (SELECT json_group_array(value) FROM json_each(json_extract(serialized_data,'$.Completed')) WHERE value <> 900000),
    'Started',   CASE WHEN EXISTS (SELECT 1 FROM json_each(json_extract(serialized_data,'$.Started')) WHERE value = 900000)
                      THEN json_extract(serialized_data,'$.Started')
                      ELSE json_insert(json_extract(serialized_data,'$.Started'), '$[#]', 900000) END,
    'Progress',  CASE WHEN EXISTS (SELECT 1 FROM json_each(json_extract(serialized_data,'$.Progress')) WHERE json_extract(value,'$.rid') = 900000)
                      THEN json_extract(serialized_data,'$.Progress')
                      ELSE json_insert(json_extract(serialized_data,'$.Progress'), '$[#]', json_object('id',900000,'rid',900000,'p',0)) END))
WHERE EXISTS (SELECT 1 FROM json_each(json_extract(serialized_data,'$.Completed')) WHERE value = 900000)
  AND NOT EXISTS (SELECT 1 FROM json_each(json_extract(serialized_data,'$.Completed')) WHERE value = 80)
  AND COALESCE(CAST(json_extract(
        CASE WHEN COALESCE((SELECT player_properties FROM players WHERE players.player_id = quest_status.player_id),'') = ''
             THEN '{}' ELSE (SELECT player_properties FROM players WHERE players.player_id = quest_status.player_id) END,
        '$."dimension.max"') AS INTEGER), 0) < 1;

COMMIT;

-- Report: who now has what.
SELECT p.player_id, p.player_name,
       json_extract(CASE WHEN COALESCE(p.player_properties,'')='' THEN '{}' ELSE p.player_properties END, '$."dimension.max"') AS dim_max,
       (SELECT 1 FROM json_each(json_extract(qs.serialized_data,'$.Completed')) WHERE value = 80)     AS kingslayer,
       (SELECT 1 FROM json_each(json_extract(qs.serialized_data,'$.Completed')) WHERE value = 900000) AS dim_quest_done,
       (SELECT 1 FROM json_each(json_extract(qs.serialized_data,'$.Started'))   WHERE value = 900000) AS dim_quest_started,
       (SELECT json_extract(value,'$.p') FROM json_each(json_extract(qs.serialized_data,'$.Progress')) WHERE json_extract(value,'$.rid') = 900000) AS dim_kill_progress
FROM players p JOIN quest_status qs ON qs.player_id = p.player_id;
