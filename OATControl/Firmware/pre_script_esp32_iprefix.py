import os

Import("env")


def cprint(*args, **kwargs):
    print('pre_script_esp32_iprefix.py:', *args, **kwargs)


def remove_prefix(text: str, prefix: str) -> str:
    if text.startswith(prefix):
        return text[len(prefix):]
    return text


def use_iprefix_for_esp32_framework(env, node):
    """
    The esp32 arduino framework
    (https://registry.platformio.org/platforms/platformio/espressif32) has too
    many -I includes, which can easily go over the 32kB Windows process command
    line limit. I consider this a bug in the framework, but we can fix it with
    this platformio middleware by using GCCs
    https://gcc.gnu.org/onlinedocs/gcc/Directory-Options.html#index-iwithprefixbefore,
    which allows us to set a -iprefix once, then reference that prefix when
    doing an -I include using -iwithprefixbefore. See
    https://github.com/OpenAstroTech/OATFWGUI/issues/62 for more details.
    """
    if 'INCPREFIX' in env and env['INCPREFIX'] != '-I':
        cprint(f"Warning: ignoring weird prefix for {node.get_abspath()}, {env['INCPREFIX']}")
        return node

    orig_include_paths = {
        'framework': [],
        'other': [],
    }
    for include_path in env['CPPPATH']:
        if 'framework-arduinoespressif32' in include_path:
            orig_include_paths['framework'].append(include_path)
        else:
            orig_include_paths['other'].append(include_path)

    # Find the common path for the framework, add on the path separator (since commonpath leaves it off)
    common_path_prefix = os.path.commonpath(orig_include_paths['framework']) + os.sep
    if len(common_path_prefix) < len('iwithprefixbefore'):
        # Only continue with replacement if we'll actually see a
        # reduction in the command length
        return node

    new_framework_include_paths = []
    for orig_include_path in orig_include_paths['framework']:
        new_framework_include_paths.append(remove_prefix(orig_include_path, common_path_prefix))

    cprint(f'{node.get_abspath()} prefix is {common_path_prefix}')

    # If just a normal list of strings, SCONS will quote the string if it has spaces
    # We don't want that, so we use a list of list of strings
    iprefix_list = [['-iprefix', common_path_prefix]]
    iprefixbefore_list = [['-iwithprefixbefore', f_i] for f_i in new_framework_include_paths]
    normal_include_list = [['-I', o_i] for o_i in orig_include_paths['other']]
    return env.Object(
        node,
        CCFLAGS=env['CCFLAGS'] + iprefix_list + iprefixbefore_list + normal_include_list,
        INCPREFIX=None,
        CPPPATH=None,
    )


env.AddBuildMiddleware(use_iprefix_for_esp32_framework)
