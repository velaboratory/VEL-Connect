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

		collection, err := dao.FindCollectionByNameOrId("_pb_users_auth_")
		if err != nil {
			return err
		}

		// add
		new_devices := &schema.SchemaField{}
		json.Unmarshal([]byte(`{
			"system": false,
			"id": "1hwaooub",
			"name": "devices",
			"type": "relation",
			"required": false,
			"unique": false,
			"options": {
				"collectionId": "fupstz47c55s69f",
				"cascadeDelete": false,
				"minSelect": null,
				"maxSelect": null,
				"displayFields": []
			}
		}`), new_devices)
		collection.Schema.AddField(new_devices)

		return dao.SaveCollection(collection)
	}, func(db dbx.Builder) error {
		dao := daos.New(db);

		collection, err := dao.FindCollectionByNameOrId("_pb_users_auth_")
		if err != nil {
			return err
		}

		// remove
		collection.Schema.RemoveField("1hwaooub")

		return dao.SaveCollection(collection)
	})
}
