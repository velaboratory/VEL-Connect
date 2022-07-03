from enum import Enum

import fastapi
from pyppeteer import launch

# APIRouter creates path operations for user module
router = fastapi.APIRouter(
    prefix="/api",
    tags=["Oculus API"],
    responses={404: {"description": "Not found"}},
)


class QuestRift(str, Enum):
    quest = "quest"
    rift = "rift"


@router.get('/get_store_details/{quest_rift}/{app_id}')
async def get_version_nums(quest_rift: QuestRift, app_id: int):
    browser = await launch(headless=True, options={'args': ['--no-sandbox']})
    page = await browser.newPage()
    await page.goto(f'https://www.oculus.com/experiences/{quest_rift}/{app_id}')

    ret = {}

    # title
    title = await page.querySelector(".app-description__title")
    ret["title"] = await page.evaluate("e => e.textContent", title)

    # description
    desc = await page.querySelector(".clamped-description__content")
    ret["description"] = await page.evaluate("e => e.textContent", desc)

    # versions
    await page.evaluate(
        "document.querySelector('.app-details-version-info-row__version').nextElementSibling.firstChild.click();")
    elements = await page.querySelectorAll('.sky-dropdown__link.link.link--clickable')

    versions = []
    for e in elements:
        v = await page.evaluate('(element) => element.textContent', e)
        versions.append({
            'channel': v.split(':')[0],
            'version': v.split(':')[1]
        })

    ret["versions"] = versions

    await browser.close()

    return ret
