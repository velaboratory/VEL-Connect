package migrations

import (
	"encoding/json"

	"github.com/pocketbase/dbx"
	"github.com/pocketbase/pocketbase/daos"
	m "github.com/pocketbase/pocketbase/migrations"
	"github.com/pocketbase/pocketbase/models/schema"
)

func init() {
	m.Register(func(db dbx.Builder) error {
		dao := daos.New(db);

		collection, err := dao.FindCollectionByNameOrId("3qwwkz4wb0lyi78")
		if err != nil {
			return err
		}

		// update
		edit_block_id := &schema.SchemaField{}
		json.Unmarshal([]byte(`{
			"system": false,
			"id": "80tmi6fm",
			"name": "block_id",
			"type": "text",
			"required": false,
			"unique": false,
			"options": {
				"min": null,
				"max": null,
				"pattern": ""
			}
		}`), edit_block_id)
		collection.Schema.AddField(edit_block_id)

		return dao.SaveCollection(collection)
	}, func(db dbx.Builder) error {
		dao := daos.New(db);

		collection, err := dao.FindCollectionByNameOrId("3qwwkz4wb0lyi78")
		if err != nil {
			return err
		}

		// update
		edit_block_id := &schema.SchemaField{}
		json.Unmarshal([]byte(`{
			"system": false,
			"id": "80tmi6fm",
			"name": "block_id",
			"type": "text",
			"required": true,
			"unique": false,
			"options": {
				"min": null,
				"max": null,
				"pattern": ""
			}
		}`), edit_block_id)
		collection.Schema.AddField(edit_block_id)

		return dao.SaveCollection(collection)
	})
}
