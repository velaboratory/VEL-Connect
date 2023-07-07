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

		json.Unmarshal([]byte(`[
			"CREATE UNIQUE INDEX ` + "`" + `idx_jgyX3xA` + "`" + ` ON ` + "`" + `Device` + "`" + ` (` + "`" + `device_id` + "`" + `)"
		]`), &collection.Indexes)

		// add
		new_device_id := &schema.SchemaField{}
		json.Unmarshal([]byte(`{
			"system": false,
			"id": "vjzi0uvv",
			"name": "device_id",
			"type": "text",
			"required": true,
			"unique": false,
			"options": {
				"min": null,
				"max": null,
				"pattern": ""
			}
		}`), new_device_id)
		collection.Schema.AddField(new_device_id)

		return dao.SaveCollection(collection)
	}, func(db dbx.Builder) error {
		dao := daos.New(db);

		collection, err := dao.FindCollectionByNameOrId("fupstz47c55s69f")
		if err != nil {
			return err
		}

		json.Unmarshal([]byte(`[]`), &collection.Indexes)

		// remove
		collection.Schema.RemoveField("vjzi0uvv")

		return dao.SaveCollection(collection)
	})
}
