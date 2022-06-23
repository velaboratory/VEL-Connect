import requests
import json
import discord
import random
from discord_slash import SlashContext, SlashCommand
from discord_slash.utils.manage_commands import create_option
from discord.ext import commands
import pandas as pd
import matplotlib.pyplot as plt
import config

user_count_url = "http://velconnect2.ugavel.com/api/get_user_count"

# guild_ids = [706393774804303924]
guild_ids = [706393774804303924,630847214511587328]

def get_prefix(client, message):
    return 'velbot'


bot = commands.Bot(command_prefix=get_prefix)
slash = SlashCommand(bot, sync_commands=True)


@bot.event
async def on_ready():
    print("Ready!")


# @bot.command(name="velbot")
# async def stats_command(ctx, *args):
#     await ctx.send(random.choice(["yes", "no"]))


@slash.slash(
    name="convrged_count",
    description="Shows a graph with the current user count in conVRged.",
    options=[
        create_option(
                name="hours",
                description="Number of hours of history to include. The default is 24 hours.",
                option_type=4,
                required=False
        )
    ],
    guild_ids=guild_ids
)
async def _convrged_count(ctx: SlashContext, hours: int = 24):
    data = get_user_count(hours)

    await post_graph(pd.DataFrame(data), ctx, "Last "+str(hours)+" hours:")



def get_user_count(graph_hours=24):
    r = requests.get(user_count_url, params={'hours': graph_hours})

    if r.status_code != 200:
        return -1
    else:
        return json.loads(r.text)


async def post_graph(df, ctx, length_message, scatter=False):
    if len(df) > 0:
        await ctx.defer()
        df['timestamp'] = pd.to_datetime(df['timestamp'])
        df.columns = ['Time', 'Player Count']

        df = df[df['Player Count'] >= 0]

        params = {"ytick.color": "w",
                  "xtick.color": "w",
                  "axes.labelcolor": "w",
                  "axes.edgecolor": "w"}
        plt.rcParams.update(params)
        fig = plt.figure(figsize=(7, 3), dpi=200)
        ax = plt.axes()
        plt.margins(x=0)

        if scatter:
            plt.scatter(x=df['Time'], y=df['Player Count'],
                        alpha=.1, s=.5, c='w')
        else:
            df.plot(ax=ax, x='Time', y='Player Count', linewidth=3.0, c='w')
            # ax.step(df['Time'], df['Player Count'], linewidth=3.0, c='w')
            ax.get_legend().remove()

        # ax.get_xaxis().set_visible(False)
        # ax.get_legend().remove()

        plt.savefig('graph.png', transparent=True, bbox_inches='tight')
        plt.draw()
        plt.clf()
        plt.close("all")

        await ctx.send(content=f"Player Count: **{df.iloc[-1]['Player Count']:,.0f}**\n{length_message}", file=discord.File('graph.png'))
    else:
        await ctx.send('No players :(')


bot.run(config.bot_token)