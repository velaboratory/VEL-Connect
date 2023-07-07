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

		collection, err := dao.FindCollectionByNameOrId("fupstz47c55s69f")
		if err != nil {
			return err
		}

		// remove
		collection.Schema.RemoveField("g5wezfiu")

		return dao.SaveCollection(collection)
	}, func(db dbx.Builder) error {
		dao := daos.New(db);

		collection, err := dao.FindCollectionByNameOrId("fupstz47c55s69f")
		if err != nil {
			return err
		}

		// add
		del_current_room_data := &schema.SchemaField{}
		json.Unmarshal([]byte(`{
			"system": false,
			"id": "g5wezfiu",
			"name": "current_room_data",
			"type": "relation",
			"required": false,
			"unique": false,
			"options": {
				"collectionId": "3qwwkz4wb0lyi78",
				"cascadeDelete": false,
				"minSelect": null,
				"maxSelect": 1,
				"displayFields": []
			}
		}`), del_current_room_data)
		collection.Schema.AddField(del_current_room_data)

		return dao.SaveCollection(collection)
	})
}
